using System;

namespace MarkerRemoval
{
    public class MarkerUtils
    {
        public static MIDI_ERROR_CODE MarkerRemoving(string inputFilePath, string outputhFilePath)
        {
            MIDI_ERROR_CODE iRetCode = MIDI_ERROR_CODE.NO_ERROR;
            try
            {
                iRetCode = CMIDIUtils.Load(inputFilePath);

                if (iRetCode != MIDI_ERROR_CODE.NO_ERROR)
                {
                    Console.WriteLine("[ERROR] Load File：{0}\n{1}", inputFilePath, iRetCode);
                    return MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR;
                }
               
                //Removal metaevent
                iRetCode = CMIDIUtils.RemovalMetaEventByMetaType(META_EVENT_TYPE.MARKER_TEXT);
                if (iRetCode != MIDI_ERROR_CODE.NO_ERROR)
                {
                    Console.WriteLine("[ERROR] Eraser MetaEvent:{0}", iRetCode);
                    return MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR;
                }
                // Save midi after removed marker
                iRetCode = CMIDIUtils.Save(outputhFilePath);
                if (iRetCode != MIDI_ERROR_CODE.NO_ERROR)
                {
                    Console.WriteLine("[ERROR] Saving Error: {0}", iRetCode);
                    return MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR;
                }

                return iRetCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return MIDI_ERROR_CODE.ERR_MIDI_FORMAT_ERROR;
            }          
        }
    }
}
