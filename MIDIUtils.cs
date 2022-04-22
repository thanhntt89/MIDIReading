// 2008. ?. ? v0.1�@            VC++6�p��DLL�̃��b�p�[���Ƃ��܂������Ȃ������̂őS�ʓI��C#�ō�蒼���B
// 2008.12. 1 v0.1 -> 0.2 BugFix Load���ɑO��ǂݍ��񂾃f�[�^���̂Ă鏈�������ꂽ
//                              Save()����EOT��������Ȃ������ꍇ�A�t������
//                              GetMeasBeatTick�ŕs���Ȓl���A��ꍇ���������̂ŏC��
// 2008.12.12 v0.2 -> 0.21      enum�`���̖߂�l��int�^����enum�^�֕ύX
//                              ���킹�� MIDI_EVENT �\���̂�Type��byte����EVENT_TYPE�^�֕ύX
// 2009. 2. 6 v0.21 -> 0.3      TrackClear��ǉ�
//                              Load()����EOT��ǂݔ�΂��悤�ɂ����BSave()�ł̓g���b�N�̍Ō��
//                              ������EOT�����悤�ɂ����B
// 2009. 4. 9 v0.3 -> 0.4       RCP�p�̃N���X CRCPFile ��ǉ��B
//                              �t�@�C���ǂݍ��� Load �� �����o�� Save �̂ݍ쐬�B
//                              �ǉ��@�\�͗v�]���o�Ă���ɂ��܂��E�E�E�B
// 2010. 3.31 v0.4 -> 0.41      ���Ԏ擾�n��ǉ� GetPlaytime GetTargetPositionTime ConvMPOStoDT 
//                              �e���|�n�֐� ConvBPMtoMetaTempo ConvMetaTempotoBPM
// 2010. 6.24 v0.41 -> 0.42     RCP_DLL�Ɏ��Ԏ擾�n��ǉ� GetPlaytime GetTargetPositionTime
// 2010. 6.28 v0.42 -> 0.43     RCP_DLL�ɏ��擾�n��ǉ� GetTotalStep
// 2010. 7. 2 v0.43 -> 0.44     RCP_DLL�Ɏ��Ԏ擾�n��ǉ� GetTargetPositionStep
// 2010.10.25 v0.44 -> 0.45     RCP_DLL�Ƀe���|�n�֐���ǉ� GetTargetPositionTempo
// 2011.12. 1 v0.45 -> 0.46     ���Ԏ擾�n��ǉ� GetTargetPositionTimeDT

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace MarkerRemoval
{
    /// <summary>
    /// MIDI�C�x���g�^�C�v
    /// </summary>
    /// <remarks>
    /// �C�x���g�̎�ނ���`���Ă���܂��B
    /// MIDI_EVENT�\���̂�Type�Ŏg�p����Ă��܂��B
    /// </remarks>
    public enum EVENT_TYPE
    {
        /// <summary>�m�[�g�I��9x</summary>
        NOTE_ON,
        /// <summary>�m�[�g�I�t9x</summary>
        NOTE_OFF,
        /// <summary>�m�[�g�I�t 8x</summary>
        NOTE_OFF8,
        /// <summary>�|���t�H�j�b�N�L�[�v���b�V���[ Ax</summary>
        P_KEY_PRESS,
        /// <summary>�R���g���[���`�F���W Bx</summary>
        CONTROL_CHANGE,
        /// <summary>�v���O�����`�F���W Cx</summary>
        PROGRAM_CHANGE,
        /// <summary>�`�����l���v���b�V���[ Dx</summary>
        CHANNEL_PRESS,
        /// <summary>�s�b�`�x���h Ex</summary>
        PITCH_BEND,
        /// <summary>�G�N�X�N���[�V�u F0</summary>
        EXCLUSIVE,
        /// <summary>���^�C�x���g FF</summary>
        META_EVENT
    };
    /// <summary>
    /// �C�x���g�}�����̎��
    /// </summary>
    /// <remarks>
    /// ADDMIDIEvent���\�b�h�̈����Ƃ��Ďg�p�B
    /// �C�x���g�}�����ɁA���ʒu�ɂ������ꍇ�A�O�ɑ}�����邩(INS_FORWARD)�A
    /// ���ɑ}�����邩(INS_BACK)�A�����ŃJ�X�^�}�C�Y���Č��߂邩(INS_CUSTOMIZE)��I������
    /// </remarks>
    public enum INS_TYPE
    {
        /// <summary>�O�}��</summary>
        INS_FORWARD,
        /// <summary>���}��</summary>
        INS_BACK,
        /// <summary>���[�U�[�J�X�^�}�C�Y</summary>
        INS_CUSTOMIZE
    };

    /// <summary>
    /// MIDI�C�x���g�p�\����
    /// </summary>
    /// <remarks>
    /// �P��MIDI�C�x���g���i�[���邽�߂̍\���́B
    /// </remarks>
    public struct MIDI_EVENT
    {
        /// <summary>
        /// �������݃t���O�Bfalse�ɂ���ƁASave���\�b�h�ł��̃C�x���g���ۑ�����Ȃ��B
        /// �����C�x���g�폜�Ɠ����̈Ӗ������B�����l��true
        /// </summary>
        public bool Write_Flg;  //�������݃t���O
        /// <summary>�ΏۃC�x���g�̑�Tick��</summary>
        public UInt32 dwDT;     //�ΏۃC�x���g�̑�Tick��
        /// <summary>�C�x���g�^�C�v�B�ڂ�����EVENT_TYPE�񋓑̂��Q��</summary>
        public EVENT_TYPE Type;        //enum EVENT_TYPE ������B�C�x���g���ʗp
        /// <summary>
        /// ���f�[�^�B8x�`Ex�̃`�����l���f�[�^�͂��ׂāA
        /// �G�N�X�N���[�V�u��F0 �ȍ~�̃f�[�^�A���^�C�x���g�̓f�[�^���B
        /// </summary>
        public byte[] ucData;   //���f�[�^
        /// <summary>���^�C�x���g�^�C�v�B0�`7F�܂ł̐��l
        /// �悭�g������
        ///  00 : �V�[�P���X�ԍ�
        ///  01 : �e�L�X�g�C�x���g
        ///  02 : �R�s�[���C�g
        ///  03 : �V�[�P���X�܂��̓g���b�N�l�[��
        ///  04 : �y�햼(Instrument Name)
        ///  05 : �̎�(Lyric)
        ///  06 : �}�[�J�[
        ///  07 : �L���[�|�C���g
        ///  2F : �G���h�I�u�g���b�N(EOT)
        ///  51 : �e���|�ݒ�
        ///  54 : SMPTE�I�t�Z�b�g
        ///  58 : ���q�L��
        ///  59 : ����
        ///  7F : �V�[�P���T�[�ŗL�̃��^�C�x���g
        ///  </summary>
        public byte ucMetaType; //���^�C�x���g�̎��
        /// <summary>�\�[�g�p�B����^�C�~���O�ł̕��ёւ��͂��̒l�����Ƃɍs��</summary>
        public int iSort;       //�\�[�g�p�B����^�C�~���O�ł̕��ёւ��͂��̒l�����Ƃɍs��
    }
    /// <summary>
    /// MIDI�ʒu�p�\����
    /// </summary>
    /// <remarks>
    /// ���ߐ��A�����ATick������Ȃ�\���́BGetMeasBeatTick�̖߂�l�Ƃ��ė��p����Ă��܂��B
    /// </remarks>
    public struct MIDI_POS
    {
        /// <summary>���ߐ�</summary>
        public UInt32 dwMeas;//���ߐ�
        /// <summary>����</summary>
        public UInt32 dwBeat;//����
        /// <summary>Tick��</summary>
        public UInt32 dwTick;//Tick��
    }

    /// <summary>
    /// �G���[�p�񋓑�(MIDI�p)
    /// </summary>
    /// <remarks>
    /// �e���\�b�h�̖߂�l����`����Ă��܂��B
    /// </remarks>
    public enum MIDI_ERROR_CODE
    {
        /// <summary>����I��</summary>
        NO_ERROR,
        /// <summary>�t�@�C����������Ȃ�</summary>
        ERR_FILE_NOT_FOUND,
        /// <summary>MIDI�t�H�[�}�b�g�G���[</summary>
        ERR_MIDI_FORMAT_ERROR,
        /// <summary>DeltaTime�l�̃~�X</summary>
        ERR_ILLEGAL_DT,
        /// <summary>�g���b�N�ԍ��~�X</summary>
        ERR_ILLEGAL_TRACK_NO,
        /// <summary>Format 0�̂��ߎ��s�ł��Ȃ�����</summary>
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
    /// MIDI�t�@�C�����������߂̃N���X
    /// </summary>
    /// <remarks>
    /// MIDI�t�@�C��(�g���qmid)���������߂̃N���X�ł��B
    /// �t�@�C���̃f�[�^����x���̂��߂̃��\�b�h����`����Ă��܂��B
    /// </remarks>
    public class CMIDIUtils
    {
        private CMIDIUtils()
        {

        }

        /// <summary>
        /// �e�C�x���g�����郊�X�g�̃��X�g�B
        /// </summary>
        /// <remarks>
        /// TrkEvent[0] �� 0�g���b�N�ڂ̃��X�g�B TrkEvent[1]�� 1�g���b�N�ڂ̃��X�g�B
        /// TrkEvent[0][0] ��0�g���b�N�ڂ�0�Ԗڂ̃C�x���g�B
        /// ���̃N���X���g�p����Ƃ��͂��̃C�x���g���삪���C���ɂȂ�B
        /// </remarks>
        private static List<List<MIDI_EVENT>> TrkEvents = new List<List<MIDI_EVENT>>(); //�C�x���g�p���X�g�̃��X�g
        private static UInt16 wFormat;     //�t�H�[�}�b�g

        /// <summary>
        /// MIDI�t�H�[�}�b�g�l�B��{�I��0��1�����Ȃ��B
        /// </summary>
        private static UInt16 MIDIFormat    //�O�����J�p �t�H�[�}�b�g�l �ǂݎ���p
        {
            get
            {
                return wFormat;
            }
        }
        private static UInt16 wTracks;     //�g���b�N��
        /// <summary>
        /// �g���b�N�����B
        /// �t�H�[�}�b�g0�̏ꍇ��1�Œ�A�t�H�[�}�b�g1��2�ȏ�ɂȂ�B
        /// </summary>
        private static UInt16 Tracks        //�O�����J�p �g���b�N�� �ǂݎ���p
        {
            get
            {
                return wTracks;
            }
        }
        private static UInt16 wDiv;        //TimeBase
        /// <summary>
        /// �^�C���x�[�X�l�B
        /// </summary>
        private static UInt16 TimeBase      //�O�����J�p TimeBase �ǂݎ���p
        {
            get
            {
                return wDiv;
            }
        }
        //iSort�l�̃C�x���g���̉��Z�l
        //���^�C�~���O(��DT�l)�ł̕��בւ��Ɏg���l
        //�܂��A100���Ȃ�悩�낤�Ǝv��(���^�C�~���O��100�ȏ���ݒ�l�ߍ��ނ͍̂l���ɂ���)
        private const int SORT_EVENTSTEP = 100;

        private static UInt16 wDLLVersion = 0x0046;
        /// <summary>
        /// DLL�o�[�W����
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
        /// MIDI�t�@�C���ǂݍ��ݏ���
        /// </summary>
        /// <remarks>
        /// �����Ńt�@�C���p�X���󂯎��A����� TrkEvent �ɓ����֐��B
        /// v0.3�ǉ��F�g���b�N�Ō��EOT(End of Track)�͓ǂݍ��܂�Ȃ��B
        /// </remarks>
        /// <param name="strMIDIFileName">�Ώ�MIDI�t�@�C���p�X</param>
        /// <returns>�G���[�R�[�h�BMIDI_ERROR_CODE�񋓑̂̒l���A��܂�
        /// <table>
        ///     <tr>
        ///         <th>�߂�l</th>
        ///         <th>����</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>����I��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_FILE_NOT_FOUND</td>
        ///         <td>�t�@�C����������Ȃ�</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR</td>
        ///         <td>MIDI�t�H�[�}�b�g�G���[</td>
        ///     </tr>
        /// </table>
        /// ����O�����������ꍇ��throw����܂�
        /// </returns>
        public static MIDI_ERROR_CODE Load(string strMIDIFileName)
        {
            if (System.IO.File.Exists(strMIDIFileName) == false)
            {
                return MIDI_ERROR_CODE.ERR_FILE_NOT_FOUND;
            }

            MIDI_ERROR_CODE iRetCode = MIDI_ERROR_CODE.NO_ERROR;
            TrkEvents.Clear();

            //MIDI�t�@�C���ǂݍ���
            BinaryReader br = null;
            try
            {
                //�t�@�C�����J��
                br = new BinaryReader(File.OpenRead(strMIDIFileName), Encoding.GetEncoding("shift-jis"));

                string strMThd;
                strMThd = Encoding.GetEncoding("shift-jis").GetString(br.ReadBytes(4));

                //�擪��MThd���m�F
                if (strMThd.Equals("MThd") == true)
                {
                    UInt16 i;
                    //MIDI�t�@�C��
                    GetDword(br);//�ǂݎ̂�
                    wFormat = GetWord(br);
                    wTracks = GetWord(br);
                    wDiv = GetWord(br);

                    //�g���b�N�ǂݍ���
                    for (i = 0; i < wTracks; i++)
                    {
                        List<MIDI_EVENT> list_MEv = new List<MIDI_EVENT>();
                        iRetCode = ReadMIDITrack(ref list_MEv, br);
                        //ReadMIDITrack�ŃG���[���A������I������B�G���[�R�[�h�͂��̂܂܏�(�Ăяo����)�֕Ԃ�
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
                    //MIDI�t�@�C���ł͂Ȃ�
                    iRetCode = MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR;
                }
            }
            catch (Exception e)
            {
                //��O�͏�֓�����
                throw e;
            }
            finally
            {
                //����
                if (br != null)
                    br.Close();
            }

            return iRetCode;
        }
        /// <summary>
        /// MIDI�t�@�C���������ݏ���
        /// </summary>
        /// <remarks>
        /// �����Ńt�@�C���p�X���󂯎��ATrkEvent �̏���mid�t�@�C���Ƃ��ď����o���B
        /// v0.3�ǉ��FLoad��EOT��ǂݍ��܂Ȃ��悤�ɂ����̂ŁA�C�x���g�̍Ō��EOT�������Œǉ�����B
        /// </remarks>
        /// <param name="strMIDIFileName">�����o���t�@�C���p�X</param>
        /// <returns>�G���[�R�[�h�BMIDI_ERROR_CODE�񋓑̂̒l���A��܂�
        /// <table>
        ///     <tr>
        ///         <th>�߂�l</th>
        ///         <th>����</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>����I��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_DT</td>
        ///         <td>��������dt�l���}�C�i�X�ɂȂ�Ƃ��̃G���[���Ԃ�܂��B�s���f�[�^�B</td>
        ///     </tr>
        /// </table>
        /// ����O�����������ꍇ��throw����܂�
        /// </returns>
        public static MIDI_ERROR_CODE Save(string strMIDIFileName)
        {
            //MIDI�t�@�C����������
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
                    objBW.Write((UInt32)0);//����0�Œ��������Ă����B��ŏ���������B
                    dwNowTrackLengthPointer = dwWriteByte;
                    dwWriteByte += 4;

                    dwNowDT = 0;
                    Running_Status = 0;
                    //1�C�x���g����������
                    foreach (MIDI_EVENT MEv in L_MEv)
                    {
                        UInt32 dwWriteDT;
                        uint WriteBytes;

                        if (MEv.Write_Flg == false)
                            continue;

                        //�O���DT�l��荡���DT�l���l��������(���я�����������)
                        if (MEv.dwDT < dwNowDT)
                        {
                            //�G���[
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
                                WriteDT(objBW, (UInt32)MEv.ucData.Length, out WriteBytes);//����
                                dwWriteByte += WriteBytes + 1;
                                objBW.Write(MEv.ucData);//���f�[�^
                                dwWriteByte += (uint)MEv.ucData.Length;
                                Running_Status = 0xf0;
                                break;
                            case EVENT_TYPE.META_EVENT:
                                objBW.Write((byte)0xff);//FF
                                objBW.Write(MEv.ucMetaType);//���^�^�C�v
                                WriteDT(objBW, (UInt32)MEv.ucData.Length, out WriteBytes);//����
                                dwWriteByte += WriteBytes + 2;
                                objBW.Write(MEv.ucData);//���f�[�^
                                dwWriteByte += (uint)MEv.ucData.Length;
                                Running_Status = 0xff;
                                if (MEv.ucMetaType == 0x2f)
                                {
                                    bEndOfTrack = true;
                                }
                                break;
                            default:
                                //�����j���O�X�e�[�^�X�L���B�O��Ɠ�����������1�o�C�g�ڂ��ȗ�
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

                    //EOT���Ȃ������ꍇ�͂���
                    if (bEndOfTrack == false)
                    {
                        objBW.Write((byte)0x00);//DT
                        objBW.Write((byte)0xff);//FF
                        objBW.Write((byte)0x2f);//���^�^�C�v
                        objBW.Write((byte)0x00);//�f�[�^�T�C�Y
                        dwWriteByte += 4;
                    }

                    if (iRetCode == MIDI_ERROR_CODE.NO_ERROR)
                    {
                        //�g���b�N�f�[�^���������߂���Ō�ɃT�C�Y����������
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
        /// �f���^�^�C������ʒu�𒲂ׂ�
        /// </summary>
        /// <remarks>
        /// �f���^�E�^�C������MIDI_POS�`��(123:4:120�Ȃ�)�ɕϊ����� 
        /// </remarks>
        /// <param name="dwDT">�ϊ�����f���^�^�C���l</param>
        /// <returns>�ϊ�����MIDI_POS�^�̈ʒu</returns>
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
                //1�g���b�N�ڂ̂݌���B
                //Format 1�f�[�^�łЂ���Ƃ�����2�g���b�N�ڈȍ~�ɔ��q�����邩������Ȃ��E�E�E
                //���[�Ȃ�����g�ݒ�������
                foreach (MIDI_EVENT MEv in L_MEv)
                {
                    //������DT�����C�x���gDT����ɂ������ꍇ�͈���DT�܂łŌv�Z����
                    if (MEv.dwDT > dwDT)
                        mpos.dwTick += dwDT - dwBeatChangeDT;
                    else
                        mpos.dwTick += MEv.dwDT - dwBeatChangeDT;
                    //�����p�ɒl�����Ă��� dwBeatChangeDT�͑O�̃C�x���g�Ƃ̍�
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

                //1Trk�ڂ���dwDT����낾�����ꍇ�͑���Ȃ�DT����⊮����
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
                break;//1�g���b�N�ڂ����݂Ȃ�
            }

            return mpos;
        }
        /// <summary>
        /// MIDI�C�x���g�ǉ�(List Ver.)
        /// </summary>
        /// <remarks>
        /// �C�x���g��ǉ����郁�\�b�h�ł��B
        /// </remarks>
        /// <param name="lEv">�ǉ�����C�x���g��������List</param>
        /// <param name="iTargetTrack">�ǉ�����g���b�NNo.(0�`)</param>
        /// <param name="type">�ǉ��^�C�v FORWARD�^BACK�^CUSTOMIZE ����I��</param>
        /// <returns>�G���[�R�[�h�BMIDI_ERROR_CODE�񋓑̂̒l���A��܂�
        /// <table>
        ///     <tr>
        ///         <th>�߂�l</th>
        ///         <th>����</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>����I��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>�g���b�N�ԍ����s��</td>
        ///     </tr>
        /// </table>
        /// ����O�����������ꍇ��throw����܂�
        /// </returns>
        public static MIDI_ERROR_CODE AddMIDIEvent(List<MIDI_EVENT> lEv, int iTargetTrack, INS_TYPE type)
        {
            int i;
            MIDI_EVENT MEv_Buffer;

            //��̃��X�g�͉������Ȃ�
            if (lEv.Count == 0)
                return MIDI_ERROR_CODE.NO_ERROR;//�ꉞ�A�Q�͂Ȃ��̂�NO_ERROR��Ԃ�
            //�͈͊O�̃g���b�N�������牽�����Ȃ�
            if ((iTargetTrack >= wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;

            try
            {

                //type����iSort�l��ς���
                //FORWARD,BACK�ł͊�{�I�ɁAlEv�ɒǉ��������Ԃɕ��ԁB
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

                //���ёւ�����
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
        /// MIDI�C�x���g�ǉ�(MIDI_EVENT ver.)
        /// </summary>
        /// <remarks>
        /// �C�x���g��ǉ����郁�\�b�h�ł��B
        /// </remarks>
        /// <param name="MEv">�ǉ�����C�x���g��������MIDI_EVENT�\����</param>
        /// <param name="iTargetTrack">�ǉ�����g���b�NNo.(0�`)</param>
        /// <param name="type">�ǉ��^�C�v FORWARD�^BACK�^CUSTOMIZE ����I��</param>
        /// <returns>�G���[�R�[�h�BMIDI_ERROR_CODE�񋓑̂̒l���A��܂�
        /// <table>
        ///     <tr>
        ///         <th>�߂�l</th>
        ///         <th>����</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>����I��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>�g���b�N�ԍ����s��</td>
        ///     </tr>
        /// </table>
        /// ����O�����������ꍇ��throw����܂�
        /// </returns>
        public static MIDI_ERROR_CODE AddMIDIEvent(MIDI_EVENT MEv, int iTargetTrack, INS_TYPE type)
        {
            //            int i;
            //            MIDI_EVENT MEv_Buffer;
            //�͈͊O�̃g���b�N�������牽�����Ȃ�
            if ((iTargetTrack >= wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;

            try
            {
                //type����iSort�l��ς���
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

                //���ёւ�����
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
        /// �g���b�N�f�[�^�̃\�[�g
        /// </summary>
        /// <remarks>
        /// dwDT�l�ŏ����ɕ��ёւ�������Bforeach���ł̃��[�v�Œ��ɌĂяo���Ȃ��悤��
        /// </remarks>
        /// <param name="iTargetTrack">���ёւ�����g���b�N�ԍ�</param>
        /// <returns>�G���[�R�[�h�BMIDI_ERROR_CODE�񋓑̂̒l���A��܂�
        /// <table>
        ///     <tr>
        ///         <th>�߂�l</th>
        ///         <th>����</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>����I��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>�g���b�N�ԍ����s��</td>
        ///     </tr>
        /// </table>
        /// ����O�����������ꍇ��throw����܂�
        /// </returns>
        public static MIDI_ERROR_CODE TrackSort(int iTargetTrack)
        {
            //�͈͊O�̃g���b�N�������牽�����Ȃ�
            if ((iTargetTrack >= wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;

            int i;
            MIDI_EVENT MEv_Buffer;

            //���ёւ�����
            MIDI_EVENT_Comparer MComp = new MIDI_EVENT_Comparer();
            TrkEvents[iTargetTrack].Sort(MComp);

            //�\�[�g���iSort�̔ԍ���U�蒼�� & EOT�폜
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

                //������A�r����EOT(���^0x2F)������������AWrite_Flg��false�ɂ��ď����o����Ȃ��悤�ɂ��Ă����B
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
        /// Format 1 ���� 0 �ɕϊ�
        /// </summary>
        /// <remarks>
        /// MIDI�t�H�[�}�b�g1����0�ɕϊ�����B���Ƃ���0�̏ꍇ�͉������Ȃ�
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
        /// �g���b�N�N���A(Format 1�p)
        /// </summary>
        /// <remarks>
        /// �w�肵���g���b�N�̃C�x���g�����ׂč폜����B
        /// TrackDelete�Ƃ̈Ⴂ�́A�g���b�N���̂��̂��Ȃ��Ȃ邩�A�v�f�݂̂Ȃ��Ȃ邩�̈Ⴂ�B
        /// </remarks>
        /// <param name="iTargetTrack">�N���A����g���b�NNo.</param>
        /// <returns>
        /// <table>
        ///     <tr>
        ///         <th>�߂�l</th>
        ///         <th>����</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>����I��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>�g���b�N�ԍ����s��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_FORMAT0</td>
        ///         <td>�t�H�[�}�b�g0�Ȃ̂ŏ����ł��Ȃ�����</td>
        ///     </tr>
        /// </table>
        /// </returns>
        public static MIDI_ERROR_CODE TrackClear(int iTargetTrack)
        {
            //�͈͊O�̃g���b�N�������牽�����Ȃ�
            if ((iTargetTrack >= wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;
            //Format0�̏ꍇ�͂ł��Ȃ�
            if (wFormat == 0)
                return MIDI_ERROR_CODE.ERR_FORMAT0;

            TrkEvents[iTargetTrack].Clear();
            return MIDI_ERROR_CODE.NO_ERROR;
        }

        /// <summary>
        /// �g���b�N�폜(Format 1�p)
        /// </summary>
        /// <remarks>
        /// �w�肵���g���b�N���폜����B
        /// RemoveAt�𗘗p���Ă���̂ŁA�폜�����g���b�N�̌���1�O�ɂ����B
        /// </remarks>
        /// <param name="iTargetTrack">�폜����g���b�NNo.</param>
        /// <returns>
        /// <table>
        ///     <tr>
        ///         <th>�߂�l</th>
        ///         <th>����</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>����I��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>�g���b�N�ԍ����s��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_FORMAT0</td>
        ///         <td>�t�H�[�}�b�g0�Ȃ̂ŏ����ł��Ȃ�����</td>
        ///     </tr>
        /// </table>
        /// </returns>
        public static MIDI_ERROR_CODE TrackDelete(int iTargetTrack)
        {
            //�͈͊O�̃g���b�N�������牽�����Ȃ�
            if ((iTargetTrack >= wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;
            //Format0�̏ꍇ�͂ł��Ȃ�
            if (wFormat == 0)
                return MIDI_ERROR_CODE.ERR_FORMAT0;

            TrkEvents.RemoveAt(iTargetTrack);
            wTracks--;
            return MIDI_ERROR_CODE.NO_ERROR;
        }

        /// <summary>
        /// �g���b�N�ǉ�(Format 1�p)
        /// </summary>
        /// <remarks>�w�肵���g���b�N�ɒǉ�����B</remarks>
        /// <param name="iTargetTrack">�ǉ�����g���b�NNo.</param>
        /// <param name="lEv">�ǉ��g���b�N�̃C�x���g���X�g</param>
        /// <returns>
        /// <table>
        ///     <tr>
        ///         <th>�߂�l</th>
        ///         <th>����</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>����I��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO</td>
        ///         <td>�g���b�N�ԍ����s��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_FORMAT0</td>
        ///         <td>�t�H�[�}�b�g0�Ȃ̂ŏ����ł��Ȃ�����</td>
        ///     </tr>
        /// </table>
        /// </returns>
        public static MIDI_ERROR_CODE TrackInsert(int iTargetTrack, List<MIDI_EVENT> lEv)
        {
            //�͈͊O�̃g���b�N�������牽�����Ȃ�
            if ((iTargetTrack > wTracks) || (iTargetTrack < 0))
                return MIDI_ERROR_CODE.ERR_ILLEGAL_TRACK_NO;
            //Format0�̏ꍇ�͂ł��Ȃ�
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
        /// �^�C���x�[�X�ϊ�
        /// </summary>
        /// <remarks>
        /// �w�肵���^�C���x�[�X�l�ɕϊ�����B�f�[�^������dwDT�l�͎����ŕ␳�����
        /// </remarks>
        /// <param name="AfterTB">�ύX���TB�l</param>
        public static void ChangeTB(UInt16 AfterTB)
        {
            //0�͏����ł��Ȃ��̂ŉ������Ȃ�
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
        /// MIDI�̉��t���Ԃ��擾
        /// </summary>
        /// <remarks>
        /// �Ώۂ�MIDI�t�@�C���̉��t���Ԃ��擾����B
        /// </remarks>
        /// <returns>���t����[ms]</returns>
        public static double GetPlaytime()
        {
            uint uiLastDT = 0;
            int iTrack, iEvent;
            double dbMillisecondPerTick = 500 / (double)wDiv;//�f�t�H�l��120bpm�Ōv�Z
            double dbReturnTime = 0.0;

            //�ŏIDT�l���擾
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

            //Track0�݂̂̃C�x���g�����ăe���|�v�Z���Ď��Ԃ��o��
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
        /// ����̈ʒu�̎��Ԃ��擾
        /// </summary>
        /// <remarks>
        /// �Ώۂ�MIDI�t�@�C���̓���̃f���^�^�C���l���_�̎��Ԃ��擾����B
        /// </remarks>
        /// <param name="ui_TargetDT">�Ώۂ̃f���^�^�C��</param>
        /// <returns>�f���^�^�C���ʒu�̎���[ms]</returns>
        public static double GetTargetPositionTime(uint ui_TargetDT)
        {
            int iEvent;
            double dbMillisecondPerTick = 500 / (double)wDiv;
            double dbReturnTime = 0.0;

            uint uiNowDT = 0;

            //Track0�݂̂̃C�x���g�����ăe���|�v�Z���Ď��Ԃ��o��
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
        /// ����̈ʒu�̎��Ԃ��擾
        /// </summary>
        /// <remarks>
        /// �Ώۂ�MIDI�t�@�C���̓���̎��Ԃ̃f���^�^�C�����擾����B
        /// GetTargetPositionTime�̋t�o�[�W�����B
        /// </remarks>
        /// <param name="dbTime">�Ώێ���[ms]</param>
        /// <returns>�f���^�^�C���l</returns>
        public static UInt32 GetTargetPositionTimeDT(double dbTime)
        {
            int iEvent;
            double dbMillisecondPerTick = 500 / (double)TimeBase;
            double dbReturnTime = 0.0;

            uint uiNowDT = 0;
            uint uiReturnDT = 0;
            bool bCalcurated = false;

            //Track0�݂̂̃C�x���g�����ăe���|�v�Z���Ď��Ԃ��o��
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
        /// ����̈ʒu�̃f���^�^�C�����擾
        /// </summary>
        /// <remarks>
        /// �Ώۂ�MIDI�t�@�C����MIDI_POS�^���f���^�^�C���l�ϊ�������B
        /// </remarks>
        /// <param name="mpos">�Ώۂ�MIDI_POS�^����</param>
        /// <returns>�f���^�^�C���ʒu</returns>
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
                //1�g���b�N�ڂ̂݌���B
                //Format 1�f�[�^�łЂ���Ƃ�����2�g���b�N�ڈȍ~�ɔ��q�����邩������Ȃ��E�E�E
                //���[�Ȃ�����g�ݒ�������
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
                break;//1�g���b�N�ڂ����݂Ȃ�
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
        /// �e���|�ϊ�
        /// </summary>
        /// <remarks>
        /// BPM����MIDI�̃e���|�l(4��������s)�ɕϊ�������B
        /// </remarks>
        /// <param name="dbTempo">BPM�l</param>
        /// <returns>���^�C�x���g0x51�pData ��byte[3]��</returns>
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
        /// �e���|�ϊ�
        /// </summary>
        /// <remarks>
        /// MIDI�̃e���|�l(4��������s)����BPM�ɕϊ�������B
        /// </remarks>
        /// <param name="byTempo">���^�C�x���g0x51�pData ��byte[3]��</param>
        /// <returns>BPM�l</returns>
        public static double ConvMetaTempoToBPM(byte[] byTempo)
        {
            double dbTempo;
            uint uiTempoValue;
            uiTempoValue = ((uint)byTempo[0] << 16) + ((uint)byTempo[1] << 8) + (uint)byTempo[2];
            dbTempo = (double)60000000.0 / uiTempoValue;
            return dbTempo;
        }

        /*������������Private�֐�*/

        /// <summary>
        /// �C�x���g�ǉ����̃\�[�g�p IComparer �N���X
        /// </summary>
        private class MIDI_EVENT_Comparer : IComparer<MIDI_EVENT>
        {
            /// <summary>
            /// ��r�pCompare�֐� �ڂ�����IComparer���Q��
            /// </summary>
            public int Compare(MIDI_EVENT x, MIDI_EVENT y)
            {
                //DT�������������ꍇ�E�E�E
                if (((int)x.dwDT - (int)y.dwDT) == 0)
                {
                    //iSort�l�ŕ��בւ�
                    return (int)x.iSort - (int)y.iSort;
                }
                else//DT���Ⴄ�ꍇ�͒P���ȕ��ёւ�
                    return (int)x.dwDT - (int)y.dwDT;
            }
        }
        /// <summary>
        /// MIDI��1�g���b�N��ǂݍ���
        /// </summary>
        /// <remarks>
        /// �v���C�x�[�g�p�̃g���b�N�ǂݏo����p�֐��B
        /// �e�g���b�N���Ƃ̃f�[�^��TrkEvent������add���ĕԂ��B
        /// </remarks>
        /// <param name="TrkEvent">�ǂݎ�����f�[�^������List��MIDI_EVENT���N���X</param>
        /// <param name="br">�ǂݎ��f�[�^�̈ʒu�ɂ��� BinaryReader</param>
        /// <returns>�G���[�R�[�h�B
        /// <table>
        ///     <tr>
        ///         <th>�߂�l</th>
        ///         <th>����</th>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.NO_ERROR</td>
        ///         <td>����I��</td>
        ///     </tr>
        ///     <tr>
        ///         <td>MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR</td>
        ///         <td>MIDI�t�H�[�}�b�g�G���[</td>
        ///     </tr>
        /// </table>
        /// </returns>
        private static MIDI_ERROR_CODE ReadMIDITrack(ref List<MIDI_EVENT> TrkEvent, BinaryReader br)
        {
            string strMTrk;
            Int32 TrackLength;
            UInt32 Sum_DT;//DT�̗݌v
            byte Running_Status = 0;//�����j���O�X�e�[�^�X
            byte ReadByteBuffer;//�ǂݎ�����o�C�g�̕ێ��p
            try
            {
                //�擪��"MTrk"���ǂ����`�F�b�N
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

                        //�G���[���p������
                        MEvent.dwDT = 0;
                        MEvent.Type = 0;
                        MEvent.ucData = new byte[1];
                        MEvent.ucMetaType = 0;
                        MEvent.Write_Flg = true;
                        MEvent.iSort = 0;
                        bEOTFlag = false;

                        //Delta_Time�ǂݍ���
                        Sum_DT += GetDT(br, out ReadBytes);
                        MEvent.dwDT = Sum_DT;
                        TrackLength -= ReadBytes;

                        //���b�Z�[�W�ǂݍ���
                        ReadByteBuffer = br.ReadByte();
                        TrackLength--;
                        //�����j���O�X�e�[�^�X�ȗ�����Ă��邩�H
                        if ((ReadByteBuffer & 0x80) == 0x80)
                        {
                            //�ȗ�����Ă��Ȃ����
                            //�����j���O�X�e�[�^�X�X�V
                            Running_Status = ReadByteBuffer;
                            if ((Running_Status & 0xf0) != 0xf0)//Ex,Meta�ȊO��
                            {
                                //�����j���O�X�e�[�^�X�ȗ�����
                                //�����𓯂��ɂ��邽�߂�1�o�C�g�ǂ�
                                ReadByteBuffer = br.ReadByte();
                                TrackLength--;
                            }
                        }

                        //���b�Z�[�W�ʕ���
                        //�����Ŏ��f�[�^���\���̂ɓ����
                        switch (Running_Status & 0xf0)
                        {
                            case 0x80://�m�[�g�I�t
                                MEvent.Type = EVENT_TYPE.NOTE_OFF8;
                                MEvent.ucData = new byte[3];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                MEvent.ucData[2] = br.ReadByte();
                                TrackLength -= 1;
                                break;
                            case 0x90://�m�[�g�I���E�I�t
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
                            case 0xa0://�|���t�H�j�b�N�L�[�v���b�V���[ Ax
                                MEvent.Type = EVENT_TYPE.P_KEY_PRESS;
                                MEvent.ucData = new byte[3];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                MEvent.ucData[2] = br.ReadByte();
                                TrackLength -= 1;
                                break;
                            case 0xb0://�R���g���[���`�F���W
                                MEvent.Type = EVENT_TYPE.CONTROL_CHANGE;
                                MEvent.ucData = new byte[3];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                MEvent.ucData[2] = br.ReadByte();
                                TrackLength -= 1;
                                break;
                            case 0xc0://�v���O�����`�F���W
                                MEvent.Type = EVENT_TYPE.PROGRAM_CHANGE;
                                MEvent.ucData = new byte[2];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                break;
                            case 0xd0://�`�����l���v���b�V���[
                                MEvent.Type = EVENT_TYPE.CHANNEL_PRESS;
                                MEvent.ucData = new byte[2];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                break;
                            case 0xe0://�s�b�`�x���h
                                MEvent.Type = EVENT_TYPE.PITCH_BEND;
                                MEvent.ucData = new byte[3];
                                MEvent.ucData[0] = Running_Status;
                                MEvent.ucData[1] = ReadByteBuffer;
                                MEvent.ucData[2] = br.ReadByte();
                                TrackLength -= 1;
                                break;
                            case 0xf0:
                                if (Running_Status == 0xf0)//�G�N�X�N���[�V�u
                                {
                                    MEvent.Type = EVENT_TYPE.EXCLUSIVE;
                                    dwExSize = GetDT(br, out ReadBytes);
                                    if (dwExSize > 0)
                                        MEvent.ucData = br.ReadBytes((int)dwExSize);
                                    else
                                        MEvent.ucData = new byte[0];
                                    TrackLength -= (Int32)(ReadBytes + dwExSize);
                                }
                                else if (Running_Status == 0xff)//���^�C�x���g
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
                        //List�ɒǉ�
                        if (!bEOTFlag)//EOT�͏������܂Ȃ�
                        {
                            TrkEvent.Add(MEvent);
                            dwDT_Before = MEvent.dwDT;
                        }
                    }

                }
                else
                {
                    //MTrk�݂���Ȃ������B
                    return MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            //MTrk�݂���Ȃ������B
            return MIDI_ERROR_CODE.NO_ERROR;
        }
        /// <summary>
        /// �r�b�N�G���f�B�A���p UInt16(WORD) �����ǂݎ��
        /// </summary>
        /// <param name="br">�ǂݎ��f�[�^�̈ʒu�ɂ��� BinaryReader</param>
        /// <returns>�ǂݎ�������l</returns>
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
        /// �r�b�N�G���f�B�A���p UInt32(DWORD) �����ǂݎ��
        /// </summary>
        /// <param name="br">�ǂݎ��f�[�^�̈ʒu�ɂ��� BinaryReader</param>
        /// <returns>�ǂݎ�������l</returns>
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
        /// �ϒ��^�̃f�[�^(delta_time�Ȃ�)���t�@�C������ǂݍ��� UInt32(DWORD) �֕ϊ�
        /// </summary>
        /// <param name="br">�ǂݎ��f�[�^�̈ʒu�ɂ��� BinaryReader</param>
        /// <param name="ReadBytes">�ǂݎ�����o�C�g��</param>
        /// <returns>�ǂݎ�������l</returns>
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
        /// UInt32��Delta_Time�f�[�^�� �ϒ�byte[] �֕ϊ����t�@�C���ɏ�������
        /// </summary>
        /// <param name="objBW">�������ރf�[�^�̈ʒu�ɂ��� BinaryWriter�I�u�W�F�N�g</param>
        /// <param name="dwWriteDT">��������DT�l</param>
        /// <param name="WriteBytes">�������񂽃o�C�g��</param>
        /// <returns>�ǂݎ�������l</returns>
        private static void WriteDT(BinaryWriter objBW, UInt32 dwWriteDT, out UInt32 WriteBytes)
        {
            byte[] WriteByteData = new byte[1];
            UInt32 Flag = 0x0fe00000;
            int i, j;

            //i�͉��o�C�g�ɂȂ邩�B
            for (i = 4; i >= 1; i--)
            {
                //0x0fe00000
                //0x001fc000
                //0x00003f80
                //0x0000007f ��4�p�^�[���� AND�����B
                //�ォ�珇�ԂɃ`�F�b�N���Ă����A0�ȊO�ɂȂ������_�� i �����̂܂܏������݃o�C�g���ɂȂ�
                if ((dwWriteDT & Flag) != 0)
                {
                    WriteByteData = new byte[i];
                    UInt32 FlagForWrite = 0x0000007f;
                    //
                    for (j = 0; j < i; j++)
                    {
                        WriteByteData[(i - 1) - j] = (byte)((dwWriteDT & FlagForWrite) >> (7 * j));
                        //�Ō�̃o�C�g�ȊO�́A�ŏ�ʃr�b�g�� 1 �ɂ���
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
