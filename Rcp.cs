using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

// 2009.05.22 追加コメント

namespace MarkerRemoval
{

    /// <summary>
    /// RCPヘッダーリズムアサイン
    /// </summary>
    /// <remarks>
    /// RCPファイルのヘッダーにある32個のリズムアサインデータ
    /// </remarks>
    public struct RCP_RHYTHM_ASSIGN
    {
        /// <summary>リズム名</summary>
        public string strRhythmName;
        /// <summary>キー</summary>
        public byte byKey;
        /// <summary>ゲートタイム</summary>
        public byte byGateTime;
    }
    /// <summary>
    /// RCPヘッダーユーザーエクスクルーシブ
    /// </summary>
    /// <remarks>
    /// RCPファイルのヘッダーにある8個のユーザーエクスクルーシブ
    /// </remarks>
    public struct RCP_USER_EXCLUSIVE
    {
        /// <summary>エクスクルーシブ名</summary>
        public string strExName;
        /// <summary>エクスクルーシブデータ</summary>
        public byte[] byExData;
    }
    /// <summary>
    /// RCPヘッダー
    /// </summary>
    /// <remarks>
    /// RCPファイルのヘッダー情報
    /// </remarks>
    public struct RCP_HEADER
    {
        /// <summary>データID 32bytes "RCM-PC98V2.0(C)COME ON MUSIC\r\n\0\0"</summary>
        public string strDataID;
        /// <summary>曲タイトル 64bytes 初期値は0x20の空白</summary>
        public string strTitle;
        /// <summary>メモ 336bytes</summary>
        public string strMemo;
        /// <summary>MIDIバスモード</summary>
        public byte[] byMIDIBusMode;
        /// <summary>タイムベース</summary>
        public uint uiTimeBase;
        /// <summary>テンポ</summary>
        public byte byTempo;
        /// <summary>拍子・分子</summary>
        public byte byBeat_M;
        /// <summary>拍子・分母</summary>
        public byte byBeat_L;
        /// <summary>曲調</summary>
        public byte byKey;
        /// <summary>プレイバイアス(キーシフト量)</summary>
        public char cKeyShift;
        /// <summary>音色コントロールファイル CM6</summary>
        public string strInstCtrlFileCM6;
        /// <summary>音色コントロールファイル GSD</summary>
        public string strInstCtrlFileGSD;
        /// <summary>トラック数</summary>
        public byte byTracks;
        /// <summary>予約領域(未使用)</summary>
        public byte[] byReserved;
        /// <summary>32個のリズムアサインデータ</summary>
        public RCP_RHYTHM_ASSIGN[] RhythmAssign;
        /// <summary>8個のユーザーエクスクルーシブデータ</summary>
        public RCP_USER_EXCLUSIVE[] UserExclusive;
    }
    /// <summary>
    /// RCPステップデータ用構造体
    /// </summary>
    /// <remarks>
    /// １つのステップデータを格納するための構造体。
    /// </remarks>
    public struct RCP_TRACK_STEPDATA
    {
        /// <summary>対象イベントの総Tick数（参考用）</summary>
        public uint uiDT;
        /// <summary>対象イベントの小節数（参考用カウント）</summary>
        public uint uiMeasure;
        /// <summary>4バイトのステップデータ</summary>
        public byte[] byStepData;  //ステップデータ
    }

    /// <summary>
    /// RCPトラックデータ用構造体
    /// </summary>
    /// <remarks>
    /// １トラックデータを格納するための構造体。
    /// </remarks>
    public struct RCP_TRACK
    {
        /// <summary>トラックナンバー</summary>
        public byte byTrackNo;
        /// <summary>トラックタイプ</summary>
        public byte byTrackType;
        /// <summary>MIDIチャンネル</summary>
        public byte byMIDIChannel;
        /// <summary>トラックごとのプレイバイアス(キーシフト量)</summary>
        public char cTrackKeyShift;
        /// <summary>STオフセット</summary>
        public byte bySTOffset;
        /// <summary>ミュートフラグ</summary>
        public byte byMute;
        /// <summary>トラック用メモ</summary>
        public string strTrackMemo;
        /// <summary>トラックデータ</summary>
        public List<RCP_TRACK_STEPDATA> TrackData;
    }

    /// <summary>
    /// エラー用列挙体(RCP用)
    /// </summary>
    /// <remarks>
    /// 各メソッドの戻り値が定義されています。
    /// </remarks>
    public enum RCP_ERROR_CODE
    {
        /// <summary>正常終了</summary>
        NO_ERROR,
        /// <summary>ファイルが見つからない</summary>
        ERR_FILE_NOT_FOUND,
        /// <summary>RCPフォーマットエラー</summary>
        ERR_RCP_FORMAT_ERROR,
        /// <summary>RCPフォーマットエラー(旧バージョン)</summary>
        ERR_RCP_OLD_VERSION,
        /// <summary>トラック番号ミス</summary>
        ERR_ILLEGAL_TRACK_NO
    }

    /// <summary>
    /// RCPファイルを扱うためのクラス
    /// </summary>
    /// <remarks>
    /// RCPファイル(拡張子rcp,r36)を扱うためのクラスです。
    /// ファイルのデータ操作支援のためのメソッドが定義されています。
    /// </remarks>
    public class CRCPFile
    {
        /// <summary>
        /// RCPヘッダー
        /// </summary>
        public RCP_HEADER RcpHeader;
        /// <summary>
        /// RCPトラックデータ
        /// </summary>
        public List<RCP_TRACK> RcpData = new List<RCP_TRACK>();

        /// <summary>
        /// コンストラクタ(初期化)
        /// </summary>
        public CRCPFile()
        {
            RcpHeader.RhythmAssign = new RCP_RHYTHM_ASSIGN[32];
            RcpHeader.UserExclusive = new RCP_USER_EXCLUSIVE[8];
            RcpHeader.byMIDIBusMode = new byte[16];
            RcpData.Clear();
        }

        /// <summary>
        /// RCPファイル読み込み処理
        /// </summary>
        /// <remarks>
        /// 引数でファイルパスを受け取り、それを RcpHeader RcpData に入れる関数。
        /// </remarks>
        /// <param name="strRCPFile">読み込むRCPファイル名</param>
        /// <returns>エラーコード。RCP_ERROR_CODE列挙体の値が帰ります
        /// <table>
        ///     <tr>
        ///         <th>戻り値</th>
        ///         <th>説明</th>
        ///     </tr>
        ///     <tr>
        ///         <td>RCP_ERROR_CODE.NO_ERROR</td>
        ///         <td>正常終了</td>
        ///     </tr>
        ///     <tr>
        ///         <td>RCP_ERROR_CODE.ERR_FILE_NOT_FOUND</td>
        ///         <td>ファイルが見つからない</td>
        ///     </tr>
        ///     <tr>
        ///         <td>RCP_ERROR_CODE.ERR_RCP_FORMAT_ERROR</td>
        ///         <td>RCPフォーマットエラー</td>
        ///     </tr>
        ///     <tr>
        ///         <td>RCP_ERROR_CODE.ERR_RCP_OLD_VERSION</td>
        ///         <td>古いRCPフォーマット(未対応)</td>
        ///     </tr>
        /// </table>
        /// ※例外が発生した場合はthrowされます
        /// </returns>
        public RCP_ERROR_CODE Load(string strRCPFile)
        {
            if (System.IO.File.Exists(strRCPFile) == false)
            {
                return RCP_ERROR_CODE.ERR_FILE_NOT_FOUND;
            }

            RCP_ERROR_CODE iRetCode = RCP_ERROR_CODE.NO_ERROR;
            RcpData.Clear();

            //RCPファイル読み込み
            BinaryReader br = null;
            try
            {
                //ファイルを開く
                br = new BinaryReader(File.OpenRead(strRCPFile), Encoding.GetEncoding("shift-jis"));

                //データID "RCM-PC98V2.0(C)COME ON MUSIC\r\n\0\0"
                RcpHeader.strDataID = Encoding.GetEncoding("shift-jis").GetString(br.ReadBytes(32));
                //合ってるか判定
                if (RcpHeader.strDataID.Equals("RCM-PC98V2.0(C)COME ON MUSIC\r\n\0\0") == true)
                {
                    int iIndex;
                    //****ヘッダ読み込み****
                    RcpHeader.strTitle = Encoding.GetEncoding("shift-jis").GetString(br.ReadBytes(64));
                    RcpHeader.strMemo = Encoding.GetEncoding("shift-jis").GetString(br.ReadBytes(336));
                    RcpHeader.byMIDIBusMode = br.ReadBytes(16);
                    RcpHeader.uiTimeBase = (uint)br.ReadByte();
                    RcpHeader.byTempo = br.ReadByte();
                    RcpHeader.byBeat_M = br.ReadByte();
                    RcpHeader.byBeat_L = br.ReadByte();
                    RcpHeader.byKey = br.ReadByte();
                    RcpHeader.cKeyShift = (char)br.ReadByte();
                    RcpHeader.strInstCtrlFileCM6 = Encoding.GetEncoding("shift-jis").GetString(br.ReadBytes(16));
                    RcpHeader.strInstCtrlFileGSD = Encoding.GetEncoding("shift-jis").GetString(br.ReadBytes(16));
                    RcpHeader.byTracks = br.ReadByte();
                    RcpHeader.uiTimeBase += ((uint)br.ReadByte()) * 256;
                    RcpHeader.byReserved = br.ReadBytes(30);

                    //バージョン違いに対応してみようとする。
                    if (RcpHeader.byTracks == 0)
                    {
                        //トラック数が0の場合、古い形式のRCPであることがわかる。
                        //通常は18か36が入る。

                        //いっその事エラーで未対応にしてしまう・・・とか
                        iRetCode = RCP_ERROR_CODE.ERR_RCP_OLD_VERSION;
                    }
                    else
                    {
                        for (iIndex = 0; iIndex < 32; iIndex++)
                        {
                            RcpHeader.RhythmAssign[iIndex].strRhythmName = Encoding.GetEncoding("shift-jis").GetString(br.ReadBytes(14));
                            RcpHeader.RhythmAssign[iIndex].byKey = br.ReadByte();
                            RcpHeader.RhythmAssign[iIndex].byGateTime = br.ReadByte();
                        }
                        for (iIndex = 0; iIndex < 8; iIndex++)
                        {
                            RcpHeader.UserExclusive[iIndex].strExName = Encoding.GetEncoding("shift-jis").GetString(br.ReadBytes(24));
                            RcpHeader.UserExclusive[iIndex].byExData = br.ReadBytes(24);
                        }

                        //トラック読み込み
                        for (iIndex = 0; iIndex < RcpHeader.byTracks; iIndex++)
                        {
                            RCP_TRACK RcpDataTmp;
                            ReadRCPTrackData(out RcpDataTmp, br);
                            RcpData.Add(RcpDataTmp);
                        }
                    }
                }
                else
                {
                    //RCPではない
                    iRetCode = RCP_ERROR_CODE.ERR_RCP_FORMAT_ERROR;
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
        /// RCPファイル書き込み処理
        /// </summary>
        /// <remarks>
        /// 引数でファイルパスを受け取り、RcpHeader RcpData の情報をRCPファイルとして書き出す。
        /// </remarks>
        /// <param name="strRCPFile">書き出すRCPファイルパス</param>
        /// <returns>エラーコード。MIDI_ERROR_CODE列挙体の値が帰ります
        /// <table>
        ///     <tr>
        ///         <th>戻り値</th>
        ///         <th>説明</th>
        ///     </tr>
        ///     <tr>
        ///         <td>RCP_ERROR_CODE.NO_ERROR</td>
        ///         <td>正常終了</td>
        ///     </tr>
        /// </table>
        /// ※例外が発生した場合はthrowされます
        /// </returns>
        public RCP_ERROR_CODE Save(string strRCPFile)
        {
                        //MIDIファイル書き込み
            BinaryWriter objBW = null;
            int i;
            int iLength;
            RCP_ERROR_CODE iRetCode = RCP_ERROR_CODE.NO_ERROR;
            try
            {
                objBW = new BinaryWriter(File.Open(strRCPFile, FileMode.Create), Encoding.GetEncoding("shift-jis"));

                //**** ソングヘッダー部 ****
                //RCPヘッダー
                objBW.Write(System.Text.Encoding.GetEncoding("shift-jis").GetBytes("RCM-PC98V2.0(C)COME ON MUSIC\r\n"));
                objBW.Write((byte)0);
                objBW.Write((byte)0);
                //タイトル
                objBW.Write(System.Text.Encoding.GetEncoding("shift-jis").GetBytes(RcpHeader.strTitle));
                iLength = System.Text.Encoding.GetEncoding("shift-jis").GetBytes(RcpHeader.strTitle).Length;
                if (iLength != 64)
                {
                    for(i = iLength;i < 64;i++)
                    {
                        objBW.Write((byte)0);
                    }
                }
                //メモ
                objBW.Write(System.Text.Encoding.GetEncoding("shift-jis").GetBytes(RcpHeader.strMemo));
                iLength = System.Text.Encoding.GetEncoding("shift-jis").GetBytes(RcpHeader.strMemo).Length;
                if (iLength != 64)
                {
                    for (i = iLength; i < 336; i++)
                    {
                        objBW.Write((byte)0);
                    }
                }
                //MIDIバスモード
                objBW.Write(RcpHeader.byMIDIBusMode);
                //タイムベース
                objBW.Write((byte)(RcpHeader.uiTimeBase & 0x000000ff));
                //Tempo
                objBW.Write(RcpHeader.byTempo);
                //拍子
                objBW.Write(RcpHeader.byBeat_M);
                objBW.Write(RcpHeader.byBeat_L);
                //調
                objBW.Write(RcpHeader.byKey);
                //プレイバイアス
                objBW.Write(RcpHeader.cKeyShift);
                //コントロールファイル名(.CM6)
                objBW.Write(System.Text.Encoding.GetEncoding("shift-jis").GetBytes(RcpHeader.strInstCtrlFileCM6));
                iLength = System.Text.Encoding.GetEncoding("shift-jis").GetBytes(RcpHeader.strInstCtrlFileCM6).Length;
                if (iLength != 16)
                {
                    for (i = iLength; i < 16; i++)
                    {
                        objBW.Write((byte)0);
                    }
                }
                //コントロールファイル名(.GSD)
                objBW.Write(System.Text.Encoding.GetEncoding("shift-jis").GetBytes(RcpHeader.strInstCtrlFileGSD));
                iLength = System.Text.Encoding.GetEncoding("shift-jis").GetBytes(RcpHeader.strInstCtrlFileGSD).Length;
                if (iLength != 16)
                {
                    for (i = iLength; i < 16; i++)
                    {
                        objBW.Write((byte)0);
                    }
                }
                //トラック数
                objBW.Write(RcpHeader.byTracks);
                //タイムベース上位ビット
                objBW.Write((byte)((RcpHeader.uiTimeBase & 0x0000ff00) >> 8));
                //予約領域(14+16bytes)
                objBW.Write(RcpHeader.byReserved);

                //リズム 32コ
                foreach (RCP_RHYTHM_ASSIGN rcprhythmassignData in RcpHeader.RhythmAssign)
                {
                    objBW.Write(System.Text.Encoding.GetEncoding("shift-jis").GetBytes(rcprhythmassignData.strRhythmName));
                    iLength = System.Text.Encoding.GetEncoding("shift-jis").GetBytes(rcprhythmassignData.strRhythmName).Length;
                    if (iLength != 14)
                    {
                        for (i = iLength; i < 14; i++)
                        {
                            objBW.Write((byte)0x20);
                        }
                    }
                    objBW.Write(rcprhythmassignData.byKey);
                    objBW.Write(rcprhythmassignData.byGateTime);
                }

                //ユーザー定義Exclusiveデータ 8コ
                foreach (RCP_USER_EXCLUSIVE rcpuserexclusiveData in RcpHeader.UserExclusive)
                {
                    objBW.Write(System.Text.Encoding.GetEncoding("shift-jis").GetBytes(rcpuserexclusiveData.strExName));
                    iLength = System.Text.Encoding.GetEncoding("shift-jis").GetBytes(rcpuserexclusiveData.strExName).Length;
                    if (iLength != 24)
                    {
                        for (i = iLength; i < 24; i++)
                        {
                            objBW.Write((byte)0x20);
                        }
                    }
                    objBW.Write(rcpuserexclusiveData.byExData);
                }

                //**** ソングヘッダー部 ここまで ****
                
                //トラック部
                int iTrack;
                for (iTrack = 0; iTrack < RcpHeader.byTracks; iTrack++)
                {
                    //トラックヘッダー
                    ushort ushDatasize;
                    ushDatasize = (ushort)(44 + (RcpData[iTrack].TrackData.Count * 4));
                    objBW.Write((ushort)ushDatasize);
                    objBW.Write(RcpData[iTrack].byTrackNo);
                    objBW.Write(RcpData[iTrack].byTrackType);
                    objBW.Write(RcpData[iTrack].byMIDIChannel);
                    objBW.Write((byte)RcpData[iTrack].cTrackKeyShift);
                    objBW.Write(RcpData[iTrack].bySTOffset);
                    objBW.Write(RcpData[iTrack].byMute);

                    objBW.Write(System.Text.Encoding.GetEncoding("shift-jis").GetBytes(RcpData[iTrack].strTrackMemo));
                    iLength = System.Text.Encoding.GetEncoding("shift-jis").GetBytes(RcpData[iTrack].strTrackMemo).Length;
                    if (iLength != 36)
                    {
                        for (i = iLength; i < 36; i++)
                        {
                            objBW.Write((byte)0x20);
                        }
                    }

                    //トラックデータ
                    foreach (RCP_TRACK_STEPDATA rcptrackstepdata_Data in RcpData[iTrack].TrackData)
                    {
                        objBW.Write(rcptrackstepdata_Data.byStepData);
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
                if (iRetCode != RCP_ERROR_CODE.NO_ERROR)
                {
                    System.IO.File.Delete(strRCPFile);
                }
            }
            return iRetCode;
        }

		/// <summary>
		/// RCPの演奏時間を取得
		/// </summary>
		/// <remarks>
		/// 対象のRCPファイルの演奏時間を取得する。
		/// </remarks>
		/// <returns>演奏時間[ms]</returns>
		public double GetPlaytime()
		{
			uint uiLastDT = 0;
			int iTrack, iEvent;
			double dbReturnTime = 0.0;
			byte byEvent;
			byte byStep;

			try
			{
				// 最初はマスタテンポで計算する
				double dbMillisecondPerStep = (double)(60000.0 / (double)this.RcpHeader.byTempo / (double)this.RcpHeader.uiTimeBase);

				RCP_TRACK track = new RCP_TRACK();

				//最終DT値を取得
				for (iTrack = 0; iTrack < this.RcpHeader.byTracks; iTrack++)
				{
					track = this.RcpData[iTrack];
					if (track.TrackData.Count > 0)
					{
						byEvent = track.TrackData[track.TrackData.Count - 1].byStepData[0];
						if (byEvent < 0xF0)
						{
							// STを持つ命令
							byStep = track.TrackData[track.TrackData.Count - 1].byStepData[1];
						}
						else
						{
							// 持たない命令
							byStep = 0;
						}

						if (uiLastDT < (track.TrackData[track.TrackData.Count - 1].uiDT + byStep))
						{
							// 最終イベントの位置＋音符の長さ
							uiLastDT = track.TrackData[track.TrackData.Count - 1].uiDT + byStep;
						}
					}
				}

				// Track17
				track = this.RcpData[16];

				uint uiNowDT = 0;

				//Track17のイベントを見てテンポ計算して時間を出す
				for (iEvent = 0; iEvent < track.TrackData.Count; iEvent++)
				{
					byEvent = track.TrackData[iEvent].byStepData[0];
					// STを持つ命令を計算の対象とする
					if (byEvent < 0xF0)
					{
						dbReturnTime += (track.TrackData[iEvent].uiDT - uiNowDT) * dbMillisecondPerStep;
						uiNowDT = track.TrackData[iEvent].uiDT;

						// TempoChange
						if (byEvent == 0xE7)
						{
							// 相対→絶対
							byte byTempo = (byte)((double)this.RcpHeader.byTempo * (double)track.TrackData[iEvent].byStepData[2] / 64.0 + 0.5);
							dbMillisecondPerStep = (double)(60000.0 / byTempo / (double)this.RcpHeader.uiTimeBase);
						}
					}
				}
				dbReturnTime += (uiLastDT - uiNowDT) * dbMillisecondPerStep;
			}
			catch (Exception)
			{
				throw;
			}

			return dbReturnTime;
		}

		/// <summary>
		/// 特定の位置の時間を取得
		/// </summary>
		/// <remarks>
		/// 対象のRCPファイルの特定のトータルステップ時点の時間を取得する。
		/// </remarks>
		/// <param name="ui_TargetDT">対象のトータルステップ</param>
		/// <returns>トータルステップ位置の時間[ms]</returns>
		public double GetTargetPositionTime(uint ui_TargetDT)
		{
			// 基本的な処理はGetPlaytimeと同じ

			int iEvent;
			double dbReturnTime = 0.0;
			uint uiNowDT = 0;
			byte byEvent;

			try
			{
				double dbMillisecondPerStep = (double)(60000.0 / (double)this.RcpHeader.byTempo / (double)this.RcpHeader.uiTimeBase);
				RCP_TRACK track = new RCP_TRACK();

				track = this.RcpData[16];

				for (iEvent = 0; iEvent < track.TrackData.Count; iEvent++)
				{
					byEvent = track.TrackData[iEvent].byStepData[0];
					if (byEvent < 0xF0)
					{
						// GetPlaytimeのui_LastDTがui_TargetDTに変わっただけ
						if (ui_TargetDT > track.TrackData[iEvent].uiDT)
						{
							dbReturnTime += (track.TrackData[iEvent].uiDT - uiNowDT) * dbMillisecondPerStep;
							uiNowDT = track.TrackData[iEvent].uiDT;
						}
						else
						{
							dbReturnTime += (ui_TargetDT - uiNowDT) * dbMillisecondPerStep;
							uiNowDT = track.TrackData[iEvent].uiDT;
							break;
						}

						if (byEvent == 0xE7)
						{
							byte byTempo = (byte)((double)this.RcpHeader.byTempo * (double)track.TrackData[iEvent].byStepData[2] / 64.0 + 0.5);
							dbMillisecondPerStep = (double)(60000.0 / byTempo / (double)this.RcpHeader.uiTimeBase);
						}
					}
				}

				if (ui_TargetDT > uiNowDT)
				{
					dbReturnTime += (ui_TargetDT - uiNowDT) * dbMillisecondPerStep;
				}
			}
			catch (Exception)
			{
				throw;
			}

			return dbReturnTime;
		}

		/// <summary>
		/// RCPのトータルステップを取得
		/// </summary>
		/// <remarks>
		/// 対象のRCPファイルのトータルステップを取得する。
		/// </remarks>
		/// <returns>演奏時間[ms]</returns>
		public uint GetTotalStep()
		{
			uint uiLastDT = 0;
			int iTrack;
			byte byEvent;
			byte byStep;

			try
			{
				RCP_TRACK track = new RCP_TRACK();

				//最終DT値を取得
				for (iTrack = 0; iTrack < this.RcpHeader.byTracks; iTrack++)
				{
					track = this.RcpData[iTrack];
					if (track.TrackData.Count > 0)
					{
						byEvent = track.TrackData[track.TrackData.Count - 1].byStepData[0];
						if (byEvent < 0xF0)
						{
							// STを持つ命令
							byStep = track.TrackData[track.TrackData.Count - 1].byStepData[1];
						}
						else
						{
							// 持たない命令
							byStep = 0;
						}

						if (uiLastDT < (track.TrackData[track.TrackData.Count - 1].uiDT + byStep))
						{
							// 最終イベントの位置＋音符の長さ
							uiLastDT = track.TrackData[track.TrackData.Count - 1].uiDT + byStep;
						}
					}
				}
			}
			catch (Exception)
			{
				throw;
			}

			return uiLastDT;
		}

		/// <summary>
		/// 特定の秒数のトータルステップを取得
		/// </summary>
		/// <remarks>
		/// 対象のRCPファイルの特定の秒数時点のトータルステップを取得する。
		/// </remarks>
		/// <param name="uiTime">対象の秒数[ms]</param>
		/// <returns>トータルステップ</returns>
		public uint GetTargetPositionStep( uint uiTime )
		{
			int iEvent;
			double dbNowTime = 0.0;
			double dbNextTime = 0.0;
			uint uiNowDT = 0;
			byte byEvent;

			try
			{
				// 1ステップ辺りの秒数[ms]
				double dbMillisecondPerStep = (double)(60000.0 / (double)this.RcpHeader.byTempo / (double)this.RcpHeader.uiTimeBase);
				RCP_TRACK track = new RCP_TRACK();

				// 調査対象はトラック17
				track = this.RcpData[16];

				for (iEvent = 0; iEvent < track.TrackData.Count; iEvent++)
				{
					byEvent = track.TrackData[iEvent].byStepData[0];
					if (byEvent < 0xF0)
					{
						// 次回イベント到達時の秒数
						dbNextTime += (track.TrackData[iEvent].uiDT - uiNowDT) * dbMillisecondPerStep;

						// 次回イベント到達時の秒数が、指定された秒数を超えない場合
						if (uiTime > (uint)dbNextTime)
						{
							dbNowTime = dbNextTime;
							uiNowDT = track.TrackData[iEvent].uiDT;
						}
						else
						{
							// 超えてしまった場合は、目的の秒数までのステップ数を加算する
							uiNowDT += (uint)( ( uiTime - (uint)dbNowTime ) / dbMillisecondPerStep );
							break;
						}

						// TempoChange
						if (byEvent == 0xE7)
						{
							byte byTempo = (byte)((double)this.RcpHeader.byTempo * (double)track.TrackData[iEvent].byStepData[2] / 64.0 + 0.5);
							dbMillisecondPerStep = (double)(60000.0 / byTempo / (double)this.RcpHeader.uiTimeBase);
						}
					}
				}
			}
			catch (Exception)
			{
				throw;
			}

			return uiNowDT;
		}

		/// <summary>
		/// 特定の位置の絶対テンポを取得
		/// </summary>
		/// <remarks>
		/// 対象のRCPファイルの特定のトータルステップ時点のテンポを取得する。
		/// </remarks>
		/// <param name="ui_TargetDT">対象のトータルステップ</param>
		/// <returns>トータルステップ位置のテンポ</returns>
		public byte GetTargetPositionTempo(uint ui_TargetDT)
		{
			int iEvent;
			byte byEvent;

			byte byTempo;

			try
			{
				RCP_TRACK track = new RCP_TRACK();

				track = this.RcpData[16];

				byTempo = this.RcpHeader.byTempo;

				for (iEvent = 0; iEvent < track.TrackData.Count; iEvent++)
				{
					byEvent = track.TrackData[iEvent].byStepData[0];
					if (byEvent < 0xF0)
					{
						if (track.TrackData[iEvent].uiDT > ui_TargetDT)
						{
							break;
						}

						if (byEvent == 0xE7)
						{
							byTempo = (byte)((double)this.RcpHeader.byTempo * (double)track.TrackData[iEvent].byStepData[2] / 64.0 + 0.5);
						}
					}
				}
			}
			catch (Exception)
			{
				throw;
			}

			return byTempo;
		}
		
		/// <summary>
        /// RCPのトラックデータ読み込み
        /// </summary>
        /// <param name="RcpTrackData"></param>
        /// <param name="br"></param>
        /// <returns></returns>
        private RCP_ERROR_CODE ReadRCPTrackData(out RCP_TRACK RcpTrackData, BinaryReader br)
        {
            UInt16 uiTrackSize;
            UInt16 uiTrackDataNum;
            UInt16 uiIndex;
            UInt32 uiTrackDT;
            uint uiMeasureCounter = 1;

            uiTrackSize = br.ReadUInt16();
            uiTrackDataNum = (UInt16)((uiTrackSize - 44) / 4);


            RcpTrackData.byTrackNo = br.ReadByte();
            //MIDI Ch. 0～15:1～16ch[MPU]  16～31:1～16ch[RS232C]  0xFF:OFF
            RcpTrackData.byTrackType = br.ReadByte();
            RcpTrackData.byMIDIChannel = br.ReadByte();
            RcpTrackData.cTrackKeyShift = (char)br.ReadByte();
            RcpTrackData.bySTOffset = br.ReadByte();
            RcpTrackData.byMute = br.ReadByte();
            RcpTrackData.strTrackMemo = Encoding.GetEncoding("shift-jis").GetString(br.ReadBytes(36));

            uiTrackDT = 0;
            RcpTrackData.TrackData = new List<RCP_TRACK_STEPDATA>();
            for (uiIndex = 0; uiIndex < uiTrackDataNum; uiIndex++)
            {
                RCP_TRACK_STEPDATA StepData;
                StepData.uiDT = uiTrackDT;
                StepData.byStepData = br.ReadBytes(4);
                //STEPを加算
                if (StepData.byStepData[0] < 0xf0)
                {
                    uiTrackDT += StepData.byStepData[1];
                }
                //小節線がきたら小節数+1する
                if (StepData.byStepData[0] == 0xfd)
                {
                    uiMeasureCounter++;
                }
                StepData.uiMeasure = uiMeasureCounter;
                RcpTrackData.TrackData.Add(StepData);
            }

            return RCP_ERROR_CODE.NO_ERROR;
        }
    }
}
