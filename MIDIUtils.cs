// 2008. ?. ? v0.1　            VC++6用のDLLのラッパーだとうまくいかなかったので全面的にC#で作り直し。
// 2008.12. 1 v0.1 -> 0.2 BugFix Load時に前回読み込んだデータを捨てる処理をいれた
//                              Save()時にEOTが見つからなかった場合、付け足す
//                              GetMeasBeatTickで不正な値が帰る場合があったので修正
// 2008.12.12 v0.2 -> 0.21      enum形式の戻り値をint型からenum型へ変更
//                              あわせて MIDI_EVENT 構造体のTypeをbyteからEVENT_TYPE型へ変更
// 2009. 2. 6 v0.21 -> 0.3      TrackClearを追加
//                              Load()時にEOTを読み飛ばすようにした。Save()ではトラックの最後に
//                              自動でEOTがつくようにした。
// 2009. 4. 9 v0.3 -> 0.4       RCP用のクラス CRCPFile を追加。
//                              ファイル読み込み Load と 書き出し Save のみ作成。
//                              追加機能は要望が出てからにします・・・。
// 2010. 3.31 v0.4 -> 0.41      時間取得系を追加 GetPlaytime GetTargetPositionTime ConvMPOStoDT 
//                              テンポ系関数 ConvBPMtoMetaTempo ConvMetaTempotoBPM
// 2010. 6.24 v0.41 -> 0.42     RCP_DLLに時間取得系を追加 GetPlaytime GetTargetPositionTime
// 2010. 6.28 v0.42 -> 0.43     RCP_DLLに情報取得系を追加 GetTotalStep
// 2010. 7. 2 v0.43 -> 0.44     RCP_DLLに時間取得系を追加 GetTargetPositionStep
// 2010.10.25 v0.44 -> 0.45     RCP_DLLにテンポ系関数を追加 GetTargetPositionTempo
// 2011.12. 1 v0.45 -> 0.46     時間取得系を追加 GetTargetPositionTimeDT

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace MarkerRemoval
{
    /// <summary>
    /// MIDIイベントタイプ
    /// </summary>
    /// <remarks>
    /// イベントの種類が定義してあります。
    /// MIDI_EVENT構造体のTypeで使用されています。
    /// </remarks>
    public enum EVENT_TYPE
    {
        /// <summary>ノートオン9x</summary>
        NOTE_ON,
        /// <summary>ノートオフ9x</summary>
        NOTE_OFF,
        /// <summary>ノートオフ 8x</summary>
        NOTE_OFF8,
        /// <summary>ポリフォニックキープレッシャー Ax</summary>
        P_KEY_PRESS,
        /// <summary>コントロールチェンジ Bx</summary>
        CONTROL_CHANGE,
        /// <summary>プログラムチェンジ Cx</summary>
        PROGRAM_CHANGE,
        /// <summary>チャンネルプレッシャー Dx</summary>
        CHANNEL_PRESS,
        /// <summary>ピッチベンド Ex</summary>
        PITCH_BEND,
        /// <summary>エクスクルーシブ F0</summary>
        EXCLUSIVE,
        /// <summary>メタイベント FF</summary>
        META_EVENT
    };
    /// <summary>
    /// イベント挿入時の種別
    /// </summary>
    /// <remarks>
    /// ADDMIDIEventメソッドの引数として使用。
    /// イベント挿入時に、同位置にあった場合、前に挿入するか(INS_FORWARD)、
    /// 後ろに挿入するか(INS_BACK)、自分でカスタマイズして決めるか(INS_CUSTOMIZE)を選択する
    /// </remarks>
    public enum INS_TYPE
    {
        /// <summary>前挿入</summary>
        INS_FORWARD,
        /// <summary>後ろ挿入</summary>
        INS_BACK,
        /// <summary>ユーザーカスタマイズ</summary>
        INS_CUSTOMIZE
    };

    /// <summary>
    /// MIDIイベント用構造体
    /// </summary>
    /// <remarks>
    /// １つのMIDIイベントを格納するための構造体。
    /// </remarks>
    public struct MIDI_EVENT
    {
        /// <summary>
        /// 書き込みフラグ。falseにすると、Saveメソッドでこのイベントが保存されない。
        /// 実質イベント削除と同等の意味を持つ。初期値はtrue
        /// </summary>
        public bool Write_Flg;  //書き込みフラグ
        /// <summary>対象イベントの総Tick数</summary>
        public UInt32 dwDT;     //対象イベントの総Tick数
        /// <summary>イベントタイプ。詳しくはEVENT_TYPE列挙体を参照</summary>
        public EVENT_TYPE Type;        //enum EVENT_TYPE が入る。イベント識別用
        /// <summary>
        /// 実データ。8x～Exのチャンネルデータはすべて、
        /// エクスクルーシブはF0 以降のデータ、メタイベントはデータ部。
        /// </summary>
        public byte[] ucData;   //実データ
        /// <summary>メタイベントタイプ。0～7Fまでの数値
        /// よく使うもの
        ///  00 : シーケンス番号
        ///  01 : テキストイベント
        ///  02 : コピーライト
        ///  03 : シーケンスまたはトラックネーム
        ///  04 : 楽器名(Instrument Name)
        ///  05 : 歌詞(Lyric)
        ///  06 : マーカー
        ///  07 : キューポイント
        ///  2F : エンドオブトラック(EOT)
        ///  51 : テンポ設定
        ///  54 : SMPTEオフセット
        ///  58 : 拍子記号
        ///  59 : 調号
        ///  7F : シーケンサー固有のメタイベント
        ///  </summary>
        public byte ucMetaType; //メタイベントの種別
        /// <summary>ソート用。同一タイミングでの並び替えはこの値をもとに行う</summary>
        public int iSort;       //ソート用。同一タイミングでの並び替えはこの値をもとに行う
    }
    /// <summary>
    /// MIDI位置用構造体
    /// </summary>
    /// <remarks>
    /// 小節数、拍数、Tick数からなる構造体。GetMeasBeatTickの戻り値として利用されています。
    /// </remarks>
    public struct MIDI_POS
    {
        /// <summary>小節数</summary>
        public UInt32 dwMeas;//小節数
        /// <summary>拍数</summary>
        public UInt32 dwBeat;//拍数
        /// <summary>Tick数</summary>
        public UInt32 dwTick;//Tick数
    }

    /// <summary>
    /// エラー用列挙体(MIDI用)
    /// </summary>
    /// <remarks>
    /// 各メソッドの戻り値が定義されています。
    /// </remarks>
    public enum MIDI_ERROR_CODE
    {
        /// <summary>正常終了</summary>
        NO_ERROR,
        /// <summary>ファイルが見つからない</summary>
        ERR_FILE_NOT_FOUND,
        /// <summary>MIDIフォーマットエラー</summary>
        ERR_MIDI_FORMAT_ERROR,
        /// <summary>DeltaTime値のミス</summary>
        ERR_ILLEGAL_DT,
        /// <summary>トラック番号ミス</summary>
        ERR_ILLEGAL_TRACK_NO,
        /// <summary>Format 0のため実行できなかった</summary>
        ERR_FORMAT0
    };

    public enum META_EVENT_TYPE : byte
    {
        SEQUENCE_NUMBER = 0x00,
        TEXT_EVENT = 0x01,
        COPYRIGHT_NOTICE = 0x02,
        SEQUENCE_OR_TRACK_NAME = 0x03,
        INSTRUMENT_NAME = 0x04,
        LYRIC_TEXT = 0x05,
        MARKER_TEXT = 0x06,
        CUE_POINT = 0x07,
        CHANNEL_PREFIX = 0x20,
        END_OF_TRACK = 0x2F,
        TEMPO_SETTING = 0x51,
        SMPTE_OFFSET = 0x54,
        TIME_SIGNATURE = 0x58,
        KEY_SIGNATURE = 0x59,
        SEQUENCER_SPECIFIC_EVENT = 0x7F
    }

    /// <summary>
    /// MIDIファイルを扱うためのクラス
    /// </summary>
    /// <remarks>
    /// MIDIファイル(拡張子mid)を扱うためのクラスです。
    /// ファイルのデータ操作支援のためのメソッドが定義されています。
    /// </remarks>
    public class CMIDIUtils
    {
        private CMIDIUtils()
        {

        }

        /// <summary>
        /// 各イベントが入るリストのリスト。
        /// </summary>
        /// <remarks>
        /// TrkEvent[0] は 0トラック目のリスト。 TrkEvent[1]は 1トラック目のリスト。
        /// TrkEvent[0][0] は0トラック目の0番目のイベント。
        /// このクラスを使用するときはこのイベント操作がメインになる。
        /// </remarks>
        private static List<List<MIDI_EVENT>> TrkEvents = new List<List<MIDI_EVENT>>(); //イベント用リストのリスト
        private static UInt16 wFormat;     //フォーマット

        /// <summary>
        /// MIDIフォーマット値。基本的に0か1しかない。
        /// </summary>
        private static UInt16 MIDIFormat    //外部公開用 フォーマット値 読み取り専用
        {
            get
            {
                return wFormat;
            }
        }
        private static UInt16 wTracks;     //トラック数
        /// <summary>
        /// トラック総数。
        /// フォーマット0の場合は1固定、フォーマット1は2以上になる。
        /// </summary>
        private static UInt16 Tracks        //外部公開用 トラック数 読み取り専用
        {
            get
            {
                return wTracks;
            }
        }
        private static UInt16 wDiv;        //TimeBase
        /// <summary>
        /// タイムベース値。
        /// </summary>
        private static UInt16 TimeBase      //外部公開用 TimeBase 読み取り専用
        {
            get
            {
                return wDiv;
            }
        }
        //iSort値のイベント毎の加算値
        //同タイミング(同DT値)での並べ替えに使う値
        //まあ、100差ならよかろうと思う(同タイミングに100以上も設定詰め込むのは考えにくい)
        private const int SORT_EVENTSTEP = 100;

        private static UInt16 wDLLVersion = 0x0046;
        /// <summary>
        /// DLLバージョン
        /// </summary>
        private static UInt16 DLLVersion
        {
            get
            {
                return wDLLVersion;
            }
        }

        /// <summary>
        /// Removal MetaEvent by MetaType
        /// </summary>
        /// <param name="mETA_EVENT_TYPE"></param>
        /// <returns></returns>
        public static MIDI_ERROR_CODE RemovalMetaEventByMetaType(META_EVENT_TYPE mETA_EVENT_TYPE)
        {
            MIDI_ERROR_CODE iRetCode = MIDI_ERROR_CODE.NO_ERROR;
            List<List<MIDI_EVENT>> delists = new List<List<MIDI_EVENT>>();
            try
            {                
                for (int i = 0; i < Tracks; i++)
                {
                    List<MIDI_EVENT> midiEvents = TrkEvents[i].Where(r => r.Type == EVENT_TYPE.META_EVENT && r.ucMetaType == (byte)mETA_EVENT_TYPE).ToList();
                    foreach (MIDI_EVENT iDI_EVENT in midiEvents)
                    {
                        TrkEvents[i].Remove(iDI_EVENT);
                    }
                }
            }
            catch
            {
                iRetCode = MIDI_ERROR_CODE.ERR_FORMAT0;
            }            

            return iRetCode;
        }

        /// <summary>
        /// MIDIファイル読み込み処理
        /// </summary>
        /// <remarks>
        /// 引数でファイルパスを受け取り、それを TrkEvent に入れる関数。
        /// v0.3追加：トラック最後のEOT(End of Track)は読み込まれない。
        /// </remarks>
        /// <param name="strMIDIFileName">対象MIDIファイルパス</param>
        /// <returns>エラーコード。MIDI_ERROR_CODE列挙体の値が帰ります
        /// <table>
        ///     <tr>
        ///         <th>戻り値</th>
        ///         <th>説明</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>正常終了</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_FILE_NOT_FOUND</td>
        ///         <td>ファイルが見つからない</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR</td>
        ///         <td>MIDIフォーマットエラー</td>
        ///     </tr>
        /// </table>
        /// ※例外が発生した場合はthrowされます
        /// </returns>
        public static MIDI_ERROR_CODE Load(string strMIDIFileName)
        {
            if (System.IO.File.Exists(strMIDIFileName) == false)
            {
                return MIDI_ERROR_CODE.ERR_FILE_NOT_FOUND;
            }

            MIDI_ERROR_CODE iRetCode = MIDI_ERROR_CODE.NO_ERROR;
            TrkEvents.Clear();

            //MIDIファイル読み込み
            BinaryReader br = null;
            try
            {
                //ファイルを開く
                br = new BinaryReader(File.OpenRead(strMIDIFileName), Encoding.GetEncoding("shift-jis"));

                string strMThd;
                strMThd = Encoding.GetEncoding("shift-jis").GetString(br.ReadBytes(4));

                //先頭のMThdを確認
                if (strMThd.Equals("MThd") == true)
                {
                    UInt16 i;
                    //MIDIファイル
                    GetDword(br);//読み捨て
                    wFormat = GetWord(br);
                    wTracks = GetWord(br);
                    wDiv = GetWord(br);

                    //トラック読み込み
                    for (i = 0; i < wTracks; i++)
                    {
                        List<MIDI_EVENT> list_MEv = new List<MIDI_EVENT>();
                        iRetCode = ReadMIDITrack(ref list_MEv, br);
                        //ReadMIDITrackでエラーが帰ったら終了する。エラーコードはそのまま上(呼び出し側)へ返す
                        if (iRetCode != MIDI_ERROR_CODE.NO_ERROR)
                        {
                            TrkEvents.Clear();
                            break;
                        }
                        TrkEvents.Add(list_MEv);
                    }
                }
                else
                {
                    //MIDIファイルではない
                    iRetCode = MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR;
                }
            }
            catch (Exception e)
            {
                //例外は上へ投げる
                throw e;
            }
            finally
            {
                //閉じる
                if (br != null)
                    br.Close();
            }

            return iRetCode;
        }
        /// <summary>
        /// MIDIファイル書き込み処理
        /// </summary>
        /// <remarks>
        /// 引数でファイルパスを受け取り、TrkEvent の情報をmidファイルとして書き出す。
        /// v0.3追加：LoadでEOTを読み込まないようにしたので、イベントの最後にEOTを自動で追加する。
        /// </remarks>
        /// <param name="strMIDIFileName">書き出すファイルパス</param>
        /// <returns>エラーコード。MIDI_ERROR_CODE列挙体の値が帰ります
        /// <table>
        ///     <tr>
        ///         <th>戻り値</th>
        ///         <th>説明</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>正常終了</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_DT</td>
        ///         <td>書き込むdt値がマイナスになるとこのエラーが返ります。不正データ。</td>
        ///     </tr>
        /// </table>
        /// ※例外が発生した場合はthrowされます
        /// </returns>
        public static MIDI_ERROR_CODE Save(string strMIDIFileName)
        {
            //MIDIファイル書き込み
            BinaryWriter objBW = null;
            UInt32 dwWriteByte = 0;
            UInt32 dwNowTrackLengthPointer;
            UInt32 dwNowDT;
            bool bEndOfTrack = false;
            MIDI_ERROR_CODE iRetCode = MIDI_ERROR_CODE.NO_ERROR;

            byte Running_Status;
            try
            {
                objBW = new BinaryWriter(File.Open(strMIDIFileName, FileMode.Create), Encoding.GetEncoding("shift-jis"));

                //Header
                objBW.Write(System.Text.Encoding.GetEncoding("shift-jis").GetBytes("MThd"));
                objBW.Write((byte)0);
                objBW.Write((byte)0);
                objBW.Write((byte)0);
                objBW.Write((byte)6);
                objBW.Write((byte)((wFormat & 0xff00) >> 8));
                objBW.Write((byte)(wFormat & 0x00ff));
                objBW.Write((byte)((wTracks & 0xff00) >> 8));
                objBW.Write((byte)(wTracks & 0x00ff));
                objBW.Write((byte)((wDiv & 0xff00) >> 8));
                objBW.Write((byte)(wDiv & 0x00ff));
                dwWriteByte = 14;

                //Track
                foreach (List<MIDI_EVENT> L_MEv in TrkEvents)
                {
                    if (iRetCode != MIDI_ERROR_CODE.NO_ERROR)
                        break;
                    objBW.Write(Encoding.GetEncoding("shift-jis").GetBytes("MTrk"));
                    dwWriteByte += 4;
                    objBW.Write((UInt32)0);//仮で0で長さを入れておく。後で書きかえる。
                    dwNowTrackLengthPointer = dwWriteByte;
                    dwWriteByte += 4;

                    dwNowDT = 0;
                    Running_Status = 0;
                    //1イベント毎書き込み
                    foreach (MIDI_EVENT MEv in L_MEv)
                    {
                        UInt32 dwWriteDT;
                        uint WriteBytes;

                        if (MEv.Write_Flg == false)
                            continue;

                        //前回のDT値より今回のDT値が値が小さい(並び順がおかしい)
                        if (MEv.dwDT < dwNowDT)
                        {
                            //エラー
                            iRetCode = MIDI_ERROR_CODE.ERR_ILLEGAL_DT;
                            break;
                        }

                        dwWriteDT = MEv.dwDT - dwNowDT;
                        dwNowDT = MEv.dwDT;

                        WriteDT(objBW, dwWriteDT, out WriteBytes);
                        dwWriteByte += WriteBytes;
                        switch (MEv.Type)
                        {
                            case EVENT_TYPE.EXCLUSIVE:
                                objBW.Write((byte)0xf0);//F0
                                WriteDT(objBW, (UInt32)MEv.ucData.Length, out WriteBytes);//長さ
                                dwWriteByte += WriteBytes + 1;
                                objBW.Write(MEv.ucData);//実データ
                                dwWriteByte += (uint)MEv.ucData.Length;
                                Running_Status = 0xf0;
                                break;
                            case EVENT_TYPE.META_EVENT:
                                objBW.Write((byte)0xff);//FF
                                objBW.Write(MEv.ucMetaType);//メタタイプ
                                WriteDT(objBW, (UInt32)MEv.ucData.Length, out WriteBytes);//長さ
                                dwWriteByte += WriteBytes + 2;
                                objBW.Write(MEv.ucData);//実データ
                                dwWriteByte += (uint)MEv.ucData.Length;
                                Running_Status = 0xff;
                                if (MEv.ucMetaType == 0x2f)
                                {
                                    bEndOfTrack = true;
                                }
                                break;
                            default:
                                //ランニングステータス有効。前回と同じだったら1バイト目を省略
                                if (Running_Status == MEv.ucData[0])
                                {
                                    byte[] ByteBuffer = new byte[MEv.ucData.Length - 1];
                                    Array.Copy(MEv.ucData, 1, ByteBuffer, 0, MEv.ucData.Length - 1);
                                    objBW.Write(ByteBuffer);
                                    dwWriteByte += (uint)MEv.ucData.Length - 1;
                                }
                                else
                                {
                                    Running_Status = MEv.ucData[0];
                                    objBW.Write(MEv.ucData);
                                    dwWriteByte += (uint)MEv.ucData.Length;
                                }
                                break;
                        }
                    }

                    //EOTがなかった場合はつける
                    if (bEndOfTrack == false)
                    {
                        objBW.Write((byte)0x00);//DT
                        objBW.Write((byte)0xff);//FF
                        objBW.Write((byte)0x2f);//メタタイプ
                        objBW.Write((byte)0x00);//データサイズ
                        dwWriteByte += 4;
                    }

                    if (iRetCode == MIDI_ERROR_CODE.NO_ERROR)
                    {
                        //トラックデータが書き込めたら最後にサイズを書き込む
                        UInt32 dwTrackBytes;
                        objBW.Seek((int)dwNowTrackLengthPointer, SeekOrigin.Begin);
                        dwTrackBytes = dwWriteByte - dwNowTrackLengthPointer - 4;
                        objBW.Write((byte)((dwTrackBytes & 0xff000000) >> 24));
                        objBW.Write((byte)((dwTrackBytes & 0x00ff0000) >> 16));
                        objBW.Write((byte)((dwTrackBytes & 0x0000ff00) >> 8));
                        objBW.Write((byte)(dwTrackBytes & 0x000000ff));
                        objBW.Seek(0, SeekOrigin.End);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (objBW != null)
                    objBW.Close();
                if (iRetCode != MIDI_ERROR_CODE.NO_ERROR)
                {
                    File.Delete(strMIDIFileName);
                }
            }
            return iRetCode;
        }
        /// <summary>
        /// デルタタイムから位置を調べる
        /// </summary>
        /// <remarks>
        /// デルタ・タイムからMIDI_POS形式(123:4:120など)に変換する 
        /// </remarks>
        /// <param name="dwDT">変換するデルタタイム値</param>
        /// <returns>変換したMIDI_POS型の位置</returns>
        public static MIDI_POS GetMeasBeatTick(UInt32 dwDT)
        {
            //            MIDI_BEATSETTING[] BeatData = new MIDI_BEATSETTING[256];
            MIDI_POS mpos;
            UInt32 dwBeatChangeDT = 0;
            uint iBeat_M = 4;
            uint iBeat_L = 4;

            mpos.dwMeas = 1;
            mpos.dwBeat = 1;
            mpos.dwTick = 0;

            foreach (List<MIDI_EVENT> L_MEv in TrkEvents)
            {
                //1トラック目のみ見る。
                //Format 1データでひょっとしたら2トラック目以降に拍子が入るかもしれない・・・
                //そーなったら組み直しかも
                foreach (MIDI_EVENT MEv in L_MEv)
                {
                    //引数のDTよりもイベントDTが先にいった場合は引数DTまでで計算する
                    if (MEv.dwDT > dwDT)
                        mpos.dwTick += dwDT - dwBeatChangeDT;
                    else
                        mpos.dwTick += MEv.dwDT - dwBeatChangeDT;
                    //差分用に値を入れておく dwBeatChangeDTは前のイベントとの差
                    dwBeatChangeDT = MEv.dwDT;

                    while (mpos.dwTick >= (uint)(wDiv * 4 / iBeat_L))
                    {
                        mpos.dwTick -= (uint)(wDiv * 4 / iBeat_L);
                        mpos.dwBeat++;
                    }
                    while (mpos.dwBeat > iBeat_M)
                    {
                        mpos.dwBeat -= iBeat_M;
                        mpos.dwMeas++;
                    }
                    if (MEv.Type == EVENT_TYPE.META_EVENT)
                    {
                        if (MEv.ucMetaType == 0x58)
                        {
                            iBeat_M = MEv.ucData[0];
                            iBeat_L = (uint)1 << MEv.ucData[1];
                        }
                    }

                    if (MEv.dwDT > dwDT)
                        break;
                }

                //1Trk目よりもdwDTが後ろだった場合は足りないDT分を補完する
                if (((int)dwDT - (int)dwBeatChangeDT) > 0)
                {
                    mpos.dwTick += (dwDT - dwBeatChangeDT);
                    while (mpos.dwTick >= (uint)(wDiv * 4 / iBeat_L))
                    {
                        mpos.dwTick -= (uint)(wDiv * 4 / iBeat_L);
                        mpos.dwBeat++;
                    }
                    while (mpos.dwBeat > iBeat_M)
                    {
                        mpos.dwBeat -= iBeat_M;
                        mpos.dwMeas++;
                    }
                }
                break;//1トラック目しかみない
            }

            return mpos;
        }
        /// <summary>
        /// MIDIイベント追加(List Ver.)
        /// </summary>
        /// <remarks>
        /// イベントを追加するメソッドです。
        /// </remarks>
        /// <param name="lEv">追加するイベントが入ったList</param>
        /// <param name="iTargetTrack">追加するトラックNo.(0～)</param>
        /// <param name="type">追加タイプ FORWARD／BACK／CUSTOMIZE から選ぶ</param>
        /// <returns>エラーコード。MIDI_ERROR_CODE列挙体の値が帰ります
        /// <table>
        ///     <tr>
        ///         <th>戻り値</th>
        ///         <th>説明</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>正常終了</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>トラック番号が不正</td>
        ///     </tr>
        /// </table>
        /// ※例外が発生した場合はthrowされます
        /// </returns>
        public static MIDI_ERROR_CODE AddMIDIEvent(List<MIDI_EVENT> lEv, int iTargetTrack, INS_TYPE type)
        {
            int i;
            MIDI_EVENT MEv_Buffer;

            //空のリストは何もしない
            if (lEv.Count == 0)
                return MIDI_ERROR_CODE.NO_ERROR;//一応、害はないのでNO_ERRORを返す
            //範囲外のトラックだったら何もしない
            if ((iTargetTrack >= wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;

            try
            {

                //type毎にiSort値を変える
                //FORWARD,BACKでは基本的に、lEvに追加した順番に並ぶ。
                switch (type)
                {
                    case INS_TYPE.INS_FORWARD:
                        for (i = 0; i < lEv.Count; i++)
                        {
                            MEv_Buffer = lEv[i];
                            MEv_Buffer.iSort = -1 - (lEv.Count - i);
                            lEv[i] = MEv_Buffer;
                        }
                        break;
                    case INS_TYPE.INS_BACK:
                        for (i = 0; i < lEv.Count; i++)
                        {
                            MEv_Buffer = lEv[i];
                            MEv_Buffer.iSort = 0x7fffffff - (lEv.Count - i);
                            lEv[i] = MEv_Buffer;
                        }
                        break;
                    case INS_TYPE.INS_CUSTOMIZE:
                        break;
                }

                //並び替え処理
                foreach (MIDI_EVENT MEv in lEv)
                {
                    TrkEvents[iTargetTrack].Add(MEv);
                }
                TrackSort(iTargetTrack);
                return MIDI_ERROR_CODE.NO_ERROR;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        /// MIDIイベント追加(MIDI_EVENT ver.)
        /// </summary>
        /// <remarks>
        /// イベントを追加するメソッドです。
        /// </remarks>
        /// <param name="MEv">追加するイベントが入ったMIDI_EVENT構造体</param>
        /// <param name="iTargetTrack">追加するトラックNo.(0～)</param>
        /// <param name="type">追加タイプ FORWARD／BACK／CUSTOMIZE から選ぶ</param>
        /// <returns>エラーコード。MIDI_ERROR_CODE列挙体の値が帰ります
        /// <table>
        ///     <tr>
        ///         <th>戻り値</th>
        ///         <th>説明</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>正常終了</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>トラック番号が不正</td>
        ///     </tr>
        /// </table>
        /// ※例外が発生した場合はthrowされます
        /// </returns>
        public static MIDI_ERROR_CODE AddMIDIEvent(MIDI_EVENT MEv, int iTargetTrack, INS_TYPE type)
        {
            //            int i;
            //            MIDI_EVENT MEv_Buffer;
            //範囲外のトラックだったら何もしない
            if ((iTargetTrack >= wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;

            try
            {
                //type毎にiSort値を変える
                switch (type)
                {
                    case INS_TYPE.INS_FORWARD:
                        MEv.iSort = -1;
                        break;
                    case INS_TYPE.INS_BACK:
                        MEv.iSort = 0x7fffffff;
                        break;
                    case INS_TYPE.INS_CUSTOMIZE:
                        break;
                }

                //並び替え処理
                TrkEvents[iTargetTrack].Add(MEv);
                TrackSort(iTargetTrack);

                return MIDI_ERROR_CODE.NO_ERROR;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// トラックデータのソート
        /// </summary>
        /// <remarks>
        /// dwDT値で昇順に並び替えをする。foreach等でのループ最中に呼び出さないように
        /// </remarks>
        /// <param name="iTargetTrack">並び替えするトラック番号</param>
        /// <returns>エラーコード。MIDI_ERROR_CODE列挙体の値が帰ります
        /// <table>
        ///     <tr>
        ///         <th>戻り値</th>
        ///         <th>説明</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>正常終了</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>トラック番号が不正</td>
        ///     </tr>
        /// </table>
        /// ※例外が発生した場合はthrowされます
        /// </returns>
        public static MIDI_ERROR_CODE TrackSort(int iTargetTrack)
        {
            //範囲外のトラックだったら何もしない
            if ((iTargetTrack >= wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;

            int i;
            MIDI_EVENT MEv_Buffer;

            //並び替え処理
            MIDI_EVENT_Comparer MComp = new MIDI_EVENT_Comparer();
            TrkEvents[iTargetTrack].Sort(MComp);

            //ソート後にiSortの番号を振り直し & EOT削除
            UInt32 dwDT_Before = 0xffffffff;
            int SortNumber = 0;
            for (i = 0; i < TrkEvents[iTargetTrack].Count; i++)
            {
                if (TrkEvents[iTargetTrack][i].dwDT == dwDT_Before)
                {
                    SortNumber += SORT_EVENTSTEP;
                    MEv_Buffer = TrkEvents[iTargetTrack][i];
                    MEv_Buffer.iSort = SortNumber;
                    TrkEvents[iTargetTrack][i] = MEv_Buffer;
                }
                else
                {
                    SortNumber = 0;
                    MEv_Buffer = TrkEvents[iTargetTrack][i];
                    MEv_Buffer.iSort = SortNumber;
                    TrkEvents[iTargetTrack][i] = MEv_Buffer;
                }
                dwDT_Before = TrkEvents[iTargetTrack][i].dwDT;

                //結合後、途中でEOT(メタ0x2F)が見つかったら、Write_Flgをfalseにして書き出されないようにしておく。
                if (i != TrkEvents[iTargetTrack].Count - 1)
                {
                    if (TrkEvents[iTargetTrack][i].Type == EVENT_TYPE.META_EVENT)
                    {
                        if (TrkEvents[iTargetTrack][i].ucMetaType == 0x2f)
                        {
                            MEv_Buffer = TrkEvents[iTargetTrack][i];
                            MEv_Buffer.Write_Flg = false;
                            TrkEvents[iTargetTrack][i] = MEv_Buffer;
                        }
                    }
                }
            }
            return MIDI_ERROR_CODE.NO_ERROR;
        }
        /// <summary>
        /// Format 1 から 0 に変換
        /// </summary>
        /// <remarks>
        /// MIDIフォーマット1から0に変換する。もともと0の場合は何もしない
        /// </remarks>
        public static void Change1to0()
        {
            int i;

            if (wFormat == 0)
                return;

            for (i = 1; i < wTracks; i++)
                AddMIDIEvent(TrkEvents[i], 0, INS_TYPE.INS_BACK);
            for (i = (wTracks - 1); i >= 1; i--)
                TrkEvents.RemoveAt(i);
            wTracks = 1;
            wFormat = 0;
        }

        /// <summary>
        /// トラッククリア(Format 1用)
        /// </summary>
        /// <remarks>
        /// 指定したトラックのイベントをすべて削除する。
        /// TrackDeleteとの違いは、トラックそのものがなくなるか、要素のみなくなるかの違い。
        /// </remarks>
        /// <param name="iTargetTrack">クリアするトラックNo.</param>
        /// <returns>
        /// <table>
        ///     <tr>
        ///         <th>戻り値</th>
        ///         <th>説明</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>正常終了</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>トラック番号が不正</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_FORMAT0</td>
        ///         <td>フォーマット0なので処理できなかった</td>
        ///     </tr>
        /// </table>
        /// </returns>
        public static MIDI_ERROR_CODE TrackClear(int iTargetTrack)
        {
            //範囲外のトラックだったら何もしない
            if ((iTargetTrack >= wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;
            //Format0の場合はできない
            if (wFormat == 0)
                return MIDI_ERROR_CODE.ERR_FORMAT0;

            TrkEvents[iTargetTrack].Clear();
            return MIDI_ERROR_CODE.NO_ERROR;
        }

        /// <summary>
        /// トラック削除(Format 1用)
        /// </summary>
        /// <remarks>
        /// 指定したトラックを削除する。
        /// RemoveAtを利用しているので、削除したトラックの後ろは1つ前にずれる。
        /// </remarks>
        /// <param name="iTargetTrack">削除するトラックNo.</param>
        /// <returns>
        /// <table>
        ///     <tr>
        ///         <th>戻り値</th>
        ///         <th>説明</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>正常終了</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>トラック番号が不正</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_FORMAT0</td>
        ///         <td>フォーマット0なので処理できなかった</td>
        ///     </tr>
        /// </table>
        /// </returns>
        public static MIDI_ERROR_CODE TrackDelete(int iTargetTrack)
        {
            //範囲外のトラックだったら何もしない
            if ((iTargetTrack >= wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;
            //Format0の場合はできない
            if (wFormat == 0)
                return MIDI_ERROR_CODE.ERR_FORMAT0;

            TrkEvents.RemoveAt(iTargetTrack);
            wTracks--;
            return MIDI_ERROR_CODE.NO_ERROR;
        }

        /// <summary>
        /// トラック追加(Format 1用)
        /// </summary>
        /// <remarks>指定したトラックに追加する。</remarks>
        /// <param name="iTargetTrack">追加するトラックNo.</param>
        /// <param name="lEv">追加トラックのイベントリスト</param>
        /// <returns>
        /// <table>
        ///     <tr>
        ///         <th>戻り値</th>
        ///         <th>説明</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>正常終了</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>トラック番号が不正</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_FORMAT0</td>
        ///         <td>フォーマット0なので処理できなかった</td>
        ///     </tr>
        /// </table>
        /// </returns>
        public static MIDI_ERROR_CODE TrackInsert(int iTargetTrack, List<MIDI_EVENT> lEv)
        {
            //範囲外のトラックだったら何もしない
            if ((iTargetTrack > wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;
            //Format0の場合はできない
            if (wFormat == 0)
                return MIDI_ERROR_CODE.ERR_FORMAT0;

            if (iTargetTrack == wTracks)
                TrkEvents.Add(lEv);
            else
                TrkEvents.Insert(iTargetTrack, lEv);

            wTracks++;
            return MIDI_ERROR_CODE.NO_ERROR;
        }
        /// <summary>
        /// タイムベース変換
        /// </summary>
        /// <remarks>
        /// 指定したタイムベース値に変換する。データ内部のdwDT値は自動で補正される
        /// </remarks>
        /// <param name="AfterTB">変更後のTB値</param>
        public static void ChangeTB(UInt16 AfterTB)
        {
            //0は処理できないので何もしない
            if (AfterTB == 0)
                return;

            double ratio;
            ratio = (double)AfterTB / (double)wDiv;

            int iTrack, iEvent;
            MIDI_EVENT MEv;
            for (iTrack = 0; iTrack < wTracks; iTrack++)
            {
                for (iEvent = 0; iEvent < TrkEvents[iTrack].Count; iEvent++)
                {
                    MEv = TrkEvents[iTrack][iEvent];
                    MEv.dwDT = (UInt32)(MEv.dwDT * ratio);
                    TrkEvents[iTrack][iEvent] = MEv;
                }
            }
            wDiv = AfterTB;
        }

        /// <summary>
        /// MIDIの演奏時間を取得
        /// </summary>
        /// <remarks>
        /// 対象のMIDIファイルの演奏時間を取得する。
        /// </remarks>
        /// <returns>演奏時間[ms]</returns>
        public static double GetPlaytime()
        {
            uint uiLastDT = 0;
            int iTrack, iEvent;
            double dbMillisecondPerTick = 500 / (double)wDiv;//デフォ値は120bpmで計算
            double dbReturnTime = 0.0;

            //最終DT値を取得
            for (iTrack = 0; iTrack < wTracks; iTrack++)
            {
                if (TrkEvents[iTrack].Count > 0)
                {
                    if (uiLastDT < TrkEvents[iTrack][TrkEvents[iTrack].Count - 1].dwDT)
                    {
                        uiLastDT = TrkEvents[iTrack][TrkEvents[iTrack].Count - 1].dwDT;
                    }
                }
            }

            uint uiNowDT = 0;

            //Track0のみのイベントを見てテンポ計算して時間を出す
            for (iEvent = 0; iEvent < TrkEvents[0].Count; iEvent++)
            {
                dbReturnTime += (TrkEvents[0][iEvent].dwDT - uiNowDT) * dbMillisecondPerTick;
                uiNowDT = TrkEvents[0][iEvent].dwDT;

                if (TrkEvents[0][iEvent].Type == EVENT_TYPE.META_EVENT)
                {
                    if (TrkEvents[0][iEvent].ucMetaType == 0x51)
                    {
                        dbMillisecondPerTick = (((uint)TrkEvents[0][iEvent].ucData[0] << 16) +
                            ((uint)TrkEvents[0][iEvent].ucData[1] << 8) +
                            ((uint)TrkEvents[0][iEvent].ucData[2])) / 1000.0 / (double)wDiv;
                    }
                }
            }
            dbReturnTime += (uiLastDT - uiNowDT) * dbMillisecondPerTick;

            return dbReturnTime;
        }

        /// <summary>
        /// 特定の位置の時間を取得
        /// </summary>
        /// <remarks>
        /// 対象のMIDIファイルの特定のデルタタイム値時点の時間を取得する。
        /// </remarks>
        /// <param name="ui_TargetDT">対象のデルタタイム</param>
        /// <returns>デルタタイム位置の時間[ms]</returns>
        public static double GetTargetPositionTime(uint ui_TargetDT)
        {
            int iEvent;
            double dbMillisecondPerTick = 500 / (double)wDiv;
            double dbReturnTime = 0.0;

            uint uiNowDT = 0;

            //Track0のみのイベントを見てテンポ計算して時間を出す
            for (iEvent = 0; iEvent < TrkEvents[0].Count; iEvent++)
            {
                if (ui_TargetDT > TrkEvents[0][iEvent].dwDT)
                {
                    dbReturnTime += (TrkEvents[0][iEvent].dwDT - uiNowDT) * dbMillisecondPerTick;
                    uiNowDT = TrkEvents[0][iEvent].dwDT;
                }
                else
                {
                    dbReturnTime += (ui_TargetDT - uiNowDT) * dbMillisecondPerTick;
                    uiNowDT = TrkEvents[0][iEvent].dwDT;
                    break;
                }

                if (TrkEvents[0][iEvent].Type == EVENT_TYPE.META_EVENT)
                {
                    if (TrkEvents[0][iEvent].ucMetaType == 0x51)
                    {
                        dbMillisecondPerTick = (((uint)TrkEvents[0][iEvent].ucData[0] << 16) +
                            ((uint)TrkEvents[0][iEvent].ucData[1] << 8) +
                            ((uint)TrkEvents[0][iEvent].ucData[2])) / 1000.0 / (double)wDiv;
                    }
                }
            }

            if (ui_TargetDT > uiNowDT)
            {
                dbReturnTime += (ui_TargetDT - uiNowDT) * dbMillisecondPerTick;
            }

            return dbReturnTime;
        }

        /// <summary>
        /// 特定の位置の時間を取得
        /// </summary>
        /// <remarks>
        /// 対象のMIDIファイルの特定の時間のデルタタイムを取得する。
        /// GetTargetPositionTimeの逆バージョン。
        /// </remarks>
        /// <param name="dbTime">対象時間[ms]</param>
        /// <returns>デルタタイム値</returns>
        public static UInt32 GetTargetPositionTimeDT(double dbTime)
        {
            int iEvent;
            double dbMillisecondPerTick = 500 / (double)TimeBase;
            double dbReturnTime = 0.0;

            uint uiNowDT = 0;
            uint uiReturnDT = 0;
            bool bCalcurated = false;

            //Track0のみのイベントを見てテンポ計算して時間を出す
            for (iEvent = 0; iEvent < TrkEvents[0].Count; iEvent++)
            {
                dbReturnTime += (TrkEvents[0][iEvent].dwDT - uiNowDT) * dbMillisecondPerTick;
                uiNowDT = TrkEvents[0][iEvent].dwDT;
                if (dbReturnTime > dbTime)
                {
                    double dbSabun = dbReturnTime - dbTime;
                    uint uiSabunDT = (uint)(dbSabun / dbMillisecondPerTick);
                    uiReturnDT = uiNowDT - uiSabunDT;
                    bCalcurated = true;
                    break;
                }

                if (TrkEvents[0][iEvent].Type == EVENT_TYPE.META_EVENT)
                {
                    if (TrkEvents[0][iEvent].ucMetaType == 0x51)
                    {
                        dbMillisecondPerTick = (((uint)TrkEvents[0][iEvent].ucData[0] << 16) +
                            ((uint)TrkEvents[0][iEvent].ucData[1] << 8) +
                            ((uint)TrkEvents[0][iEvent].ucData[2])) / 1000.0 / (double)TimeBase;
                    }
                }
            }

            if (!bCalcurated)
            {
                double dbSabun = dbTime - dbReturnTime;
                uint uiSabunDT = (uint)(dbSabun / dbMillisecondPerTick);
                uiReturnDT = uiNowDT + uiSabunDT;
            }

            return uiReturnDT;
        }


        /// <summary>
        /// 特定の位置のデルタタイムを取得
        /// </summary>
        /// <remarks>
        /// 対象のMIDIファイルのMIDI_POS型→デルタタイム値変換をする。
        /// </remarks>
        /// <param name="mpos">対象のMIDI_POS型時間</param>
        /// <returns>デルタタイム位置</returns>
        public static uint ConvMPOStoDT(MIDI_POS mpos)
        {
            uint iBeat_M = 4;
            uint iBeat_L = 4;
            uint uiBeforeDT = 0;
            uint uiPassDT = 0;
            uint uiReturnValueDT = 0;
            MIDI_POS mposTmp;
            mposTmp.dwMeas = 1;
            mposTmp.dwBeat = 1;
            mposTmp.dwTick = 0;
            bool bFinish = false;


            foreach (List<MIDI_EVENT> L_MEv in TrkEvents)
            {
                //1トラック目のみ見る。
                //Format 1データでひょっとしたら2トラック目以降に拍子が入るかもしれない・・・
                //そーなったら組み直しかも
                foreach (MIDI_EVENT MEv in L_MEv)
                {
                    uiPassDT += MEv.dwDT - uiBeforeDT;
                    while (uiPassDT >= (wDiv * 4 / iBeat_L * iBeat_M))
                    {
                        if (mpos.dwMeas > mposTmp.dwMeas)
                        {
                            mposTmp.dwMeas++;
                            uiPassDT -= (wDiv * (uint)4 / iBeat_L * iBeat_M);
                            uiReturnValueDT += (wDiv * (uint)4 / iBeat_L * iBeat_M);
                        }
                        else
                        {
                            while (uiPassDT >= (wDiv * 4 / iBeat_L))
                            {
                                if (mpos.dwBeat > mposTmp.dwBeat)
                                {
                                    mposTmp.dwBeat++;
                                    uiPassDT -= (wDiv * (uint)4 / iBeat_L);
                                    uiReturnValueDT += (wDiv * (uint)4 / iBeat_L);
                                }
                                else
                                {
                                    uiPassDT -= mpos.dwTick;
                                    uiReturnValueDT += mpos.dwTick;
                                    bFinish = true;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                    if (MEv.Type == EVENT_TYPE.META_EVENT)
                    {
                        if (MEv.ucMetaType == 0x58)
                        {
                            iBeat_M = MEv.ucData[0];
                            iBeat_L = (uint)1 << MEv.ucData[1];
                        }
                    }
                    uiBeforeDT = MEv.dwDT;
                    if (bFinish == true)
                        break;
                }
                break;//1トラック目しかみない
            }

            if (bFinish == false)
            {
                uiReturnValueDT += (wDiv * (uint)4 / iBeat_L * iBeat_M) * (mpos.dwMeas - mposTmp.dwMeas);
                if (mpos.dwBeat > mposTmp.dwBeat)
                    uiReturnValueDT += (wDiv * (uint)4 / iBeat_L) * (mpos.dwBeat - mposTmp.dwBeat);
                else
                    uiReturnValueDT -= (wDiv * (uint)4 / iBeat_L) * (mposTmp.dwBeat - mpos.dwBeat);
                uiReturnValueDT += mpos.dwTick;
            }
            return uiReturnValueDT;
        }

        /// <summary>
        /// テンポ変換
        /// </summary>
        /// <remarks>
        /// BPMからMIDIのテンポ値(4分音符μs)に変換をする。
        /// </remarks>
        /// <param name="dbTempo">BPM値</param>
        /// <returns>メタイベント0x51用Data ＜byte[3]＞</returns>
        public static byte[] ConvBPMtoMetaTempo(double dbTempo)
        {
            uint uiTempoValue;
            byte[] byTempo = new byte[3];
            uiTempoValue = (uint)(60000000 / dbTempo);
            byTempo[0] = (byte)((uiTempoValue & 0x00ff0000) >> 16);
            byTempo[1] = (byte)((uiTempoValue & 0x0000ff00) >> 8);
            byTempo[2] = (byte)(uiTempoValue & 0x000000ff);
            return byTempo;
        }

        /// <summary>
        /// テンポ変換
        /// </summary>
        /// <remarks>
        /// MIDIのテンポ値(4分音符μs)からBPMに変換をする。
        /// </remarks>
        /// <param name="byTempo">メタイベント0x51用Data ＜byte[3]＞</param>
        /// <returns>BPM値</returns>
        public static double ConvMetaTempoToBPM(byte[] byTempo)
        {
            double dbTempo;
            uint uiTempoValue;
            uiTempoValue = ((uint)byTempo[0] << 16) + ((uint)byTempo[1] << 8) + (uint)byTempo[2];
            dbTempo = (double)60000000.0 / uiTempoValue;
            return dbTempo;
        }

        /*↓↓ここからPrivate関数*/

        /// <summary>
        /// イベント追加時のソート用 IComparer クラス
        /// </summary>
        private class MIDI_EVENT_Comparer : IComparer<MIDI_EVENT>
        {
            /// <summary>
            /// 比較用Compare関数 詳しくはIComparerを参照
            /// </summary>
            public int Compare(MIDI_EVENT x, MIDI_EVENT y)
            {
                //DTが同じだった場合・・・
                if (((int)x.dwDT - (int)y.dwDT) == 0)
                {
                    //iSort値で並べ替え
                    return (int)x.iSort - (int)y.iSort;
                }
                else//DTが違う場合は単純な並び替え
                    return (int)x.dwDT - (int)y.dwDT;
            }
        }
        /// <summary>
        /// MIDIの1トラックを読み込む
        /// </summary>
        /// <remarks>
        /// プライベート用のトラック読み出し専用関数。
        /// 各トラックごとのデータをTrkEvent引数にaddして返す。
        /// </remarks>
        /// <param name="TrkEvent">読み取ったデータが入るList＜MIDI_EVENT＞クラス</param>
        /// <param name="br">読み取るデータの位置にある BinaryReader</param>
        /// <returns>エラーコード。
        /// <table>
        ///     <tr>
        ///         <th>戻り値</th>
        ///         <th>説明</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>正常終了</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR</td>
        ///         <td>MIDIフォーマットエラー</td>
        ///     </tr>
        /// </table>
        /// </returns>
        private static MIDI_ERROR_CODE ReadMIDITrack(ref List<MIDI_EVENT> TrkEvent, BinaryReader br)
        {
            string strMTrk;
            Int32 TrackLength;
            UInt32 Sum_DT;//DTの累計
            byte Running_Status = 0;//ランニングステータス
            byte ReadByteBuffer;//読み取ったバイトの保持用
            try
            {
                //先頭が"MTrk"かどうかチェック
                strMTrk = Encoding.GetEncoding("shift-jis").GetString(br.ReadBytes(4));
                if (strMTrk.Equals("MTrk") == true)
                {
                    Sum_DT = 0;
                    TrackLength = (int)GetDword(br);

                    UInt32 dwDT_Before = 0xffffffff;
                    int SortNumber = 0;

                    while (TrackLength > 0)
                    {
                        MIDI_EVENT MEvent;
                        Int32 ReadBytes;
                        UInt32 dwExSize;
                        bool bEOTFlag;

                        //エラー回避用初期化
                        MEvent.dwDT = 0;
                        MEvent.Type = 0;
                        MEvent.ucData = new byte[1];
                        MEvent.ucMetaType = 0;
                        MEvent.Write_Flg = true;
                        MEvent.iSort = 0;
                        bEOTFlag = false;

                        //Delta_Time読み込み
                        Sum_DT += GetDT(br, out ReadBytes);
                        MEvent.dwDT = Sum_DT;
                        TrackLength -= ReadBytes;

                        //メッセージ読み込み
                        ReadByteBuffer = br.ReadByte();
                        TrackLength--;
                        //ランニングステータス省略されているか？
                        if ((ReadByteBuffer & 0x80) == 0x80)
                        {
                            //省略されていなければ
                            //ランニングステータス更新
                            Running_Status = ReadByteBuffer;
                            if ((Running_Status & 0xf0) != 0xf0)//Ex,Meta以外で
                            {
                                //ランニングステータス省略時と
                                //条件を同じにするために1バイト読む
                                ReadByteBuffer = br.ReadByte();
                                TrackLength--;
                            }
                        }

                        //メッセージ別分岐
                        //ここで実データを構造体に入れる
                        switch (Running_Status & 0xf0)
                        {
                            case 0x80://ノートオフ
                                MEvent.Type = EVENT_TYPE.NOTE_OFF8;
                                MEvent.ucData = new byte[3];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                MEvent.ucData[2] = br.ReadByte();
                                TrackLength -= 1;
                                break;
                            case 0x90://ノートオン・オフ
                                MEvent.ucData = new byte[3];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                MEvent.ucData[2] = br.ReadByte();
                                if (MEvent.ucData[2] > 0)
                                    MEvent.Type = EVENT_TYPE.NOTE_ON;
                                else
                                    MEvent.Type = EVENT_TYPE.NOTE_OFF;
                                TrackLength -= 1;
                                break;
                            case 0xa0://ポリフォニックキープレッシャー Ax
                                MEvent.Type = EVENT_TYPE.P_KEY_PRESS;
                                MEvent.ucData = new byte[3];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                MEvent.ucData[2] = br.ReadByte();
                                TrackLength -= 1;
                                break;
                            case 0xb0://コントロールチェンジ
                                MEvent.Type = EVENT_TYPE.CONTROL_CHANGE;
                                MEvent.ucData = new byte[3];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                MEvent.ucData[2] = br.ReadByte();
                                TrackLength -= 1;
                                break;
                            case 0xc0://プログラムチェンジ
                                MEvent.Type = EVENT_TYPE.PROGRAM_CHANGE;
                                MEvent.ucData = new byte[2];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                break;
                            case 0xd0://チャンネルプレッシャー
                                MEvent.Type = EVENT_TYPE.CHANNEL_PRESS;
                                MEvent.ucData = new byte[2];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                break;
                            case 0xe0://ピッチベンド
                                MEvent.Type = EVENT_TYPE.PITCH_BEND;
                                MEvent.ucData = new byte[3];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                MEvent.ucData[2] = br.ReadByte();
                                TrackLength -= 1;
                                break;
                            case 0xf0:
                                if (Running_Status == 0xf0)//エクスクルーシブ
                                {
                                    MEvent.Type = EVENT_TYPE.EXCLUSIVE;
                                    dwExSize = GetDT(br, out ReadBytes);
                                    if (dwExSize > 0)
                                        MEvent.ucData = br.ReadBytes((int)dwExSize);
                                    else
                                        MEvent.ucData = new byte[0];
                                    TrackLength -= (Int32)(ReadBytes + dwExSize);
                                }
                                else if (Running_Status == 0xff)//メタイベント
                                {
                                    MEvent.Type = EVENT_TYPE.META_EVENT;
                                    MEvent.ucMetaType = br.ReadByte();
                                    dwExSize = GetDT(br, out ReadBytes);
                                    if (dwExSize > 0)
                                        MEvent.ucData = br.ReadBytes((int)dwExSize);
                                    else
                                        MEvent.ucData = new byte[0];
                                    TrackLength -= (Int32)(ReadBytes + dwExSize + 1);

                                    if (MEvent.ucMetaType == 0x2f)
                                    {
                                        bEOTFlag = true;
                                    }

                                }
                                break;
                        }
                        //iSort
                        if (dwDT_Before == MEvent.dwDT)
                        {
                            SortNumber += SORT_EVENTSTEP;
                            MEvent.iSort = SortNumber;
                        }
                        else
                        {
                            MEvent.iSort = 0;
                            SortNumber = 0;
                        }
                        //Listに追加
                        if (!bEOTFlag)//EOTは書きこまない
                        {
                            TrkEvent.Add(MEvent);
                            dwDT_Before = MEvent.dwDT;
                        }
                    }

                }
                else
                {
                    //MTrkみつからなかった。
                    return MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            //MTrkみつからなかった。
            return MIDI_ERROR_CODE.NO_ERROR;
        }
        /// <summary>
        /// ビックエンディアン用 UInt16(WORD) 整数読み取り
        /// </summary>
        /// <param name="br">読み取るデータの位置にある BinaryReader</param>
        /// <returns>読み取った数値</returns>
        private static UInt16 GetWord(BinaryReader br)
        {
            UInt16 uiRet = 0;
            try
            {
                uiRet = br.ReadByte();
                uiRet <<= 8;
                uiRet |= Convert.ToUInt16(br.ReadByte());
            }
            catch (Exception e)
            {
                throw e;
            }
            return uiRet;
        }
        /// <summary>
        /// ビックエンディアン用 UInt32(DWORD) 整数読み取り
        /// </summary>
        /// <param name="br">読み取るデータの位置にある BinaryReader</param>
        /// <returns>読み取った数値</returns>
        private static UInt32 GetDword(BinaryReader br)
        {
            UInt32 uiRet = 0;
            try
            {
                uiRet = Convert.ToUInt32(br.ReadByte()) << 24;
                uiRet |= Convert.ToUInt32(br.ReadByte()) << 16;
                uiRet |= Convert.ToUInt32(br.ReadByte()) << 8;
                uiRet |= Convert.ToUInt32(br.ReadByte());
            }
            catch (Exception e)
            {
                throw e;
            }
            return uiRet;
        }
        /// <summary>
        /// 可変長型のデータ(delta_timeなど)をファイルから読み込み UInt32(DWORD) へ変換
        /// </summary>
        /// <param name="br">読み取るデータの位置にある BinaryReader</param>
        /// <param name="ReadBytes">読み取ったバイト数</param>
        /// <returns>読み取った数値</returns>
        private static UInt32 GetDT(BinaryReader br, out Int32 ReadBytes)
        {
            UInt32 uiRet = 0;
            try
            {
                byte DataByte;
                ReadBytes = 0;
                do
                {
                    uiRet <<= 7;
                    DataByte = br.ReadByte();
                    ReadBytes++;
                    uiRet += (UInt32)(DataByte & 0x7f);
                } while ((DataByte & 0x80) == 0x80);
            }
            catch (Exception e)
            {
                throw e;
            }

            return uiRet;
        }
        /// <summary>
        /// UInt32のDelta_Timeデータを 可変長byte[] へ変換しファイルに書き込む
        /// </summary>
        /// <param name="objBW">書き込むデータの位置にある BinaryWriterオブジェクト</param>
        /// <param name="dwWriteDT">書き込むDT値</param>
        /// <param name="WriteBytes">書き込んたバイト数</param>
        /// <returns>読み取った数値</returns>
        private static void WriteDT(BinaryWriter objBW, UInt32 dwWriteDT, out UInt32 WriteBytes)
        {
            byte[] WriteByteData = new byte[1];
            UInt32 Flag = 0x0fe00000;
            int i, j;

            //iは何バイトになるか。
            for (i = 4; i >= 1; i--)
            {
                //0x0fe00000
                //0x001fc000
                //0x00003f80
                //0x0000007f の4パターンで ANDを取る。
                //上から順番にチェックしていき、0以外になった時点の i がそのまま書き込みバイト数になる
                if ((dwWriteDT & Flag) != 0)
                {
                    WriteByteData = new byte[i];
                    UInt32 FlagForWrite = 0x0000007f;
                    //
                    for (j = 0; j < i; j++)
                    {
                        WriteByteData[(i - 1) - j] = (byte)((dwWriteDT & FlagForWrite) >> (7 * j));
                        //最後のバイト以外は、最上位ビットを 1 にする
                        if (j != 0)
                            WriteByteData[(i - 1) - j] |= 0x80;
                        FlagForWrite <<= 7;
                    }
                    break;
                }
                else
                {
                    Flag >>= 7;
                }
            }

            objBW.Write(WriteByteData);
            WriteBytes = (uint)WriteByteData.Length;
            return;
        }
    }

}
