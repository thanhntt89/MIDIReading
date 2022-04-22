using System;
using System.IO;
using System.Reflection;

namespace MarkerRemoval
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            args = new string[2];
            args[0] = @"E:\Jimmii\Projects\2022\Xing\2695_MIDIマーカー削除ツール開発\SampleData\VG191998W.MID";//MakerEraser.pdb
            args[1] = @"E:\Jimmii\Projects\2022\Xing\2695_MIDIマーカー削除ツール開発\SampleData\VG191998W_out.mid";
#endif
            string fileOutPutPath = string.Empty;
            string fileInPutPath = string.Empty;
            string fileOutPutName = string.Empty;

            int iResult = 0;
            try
            {
                // 引数チェック
                if (args.Length != 2 || string.IsNullOrWhiteSpace(args[1]))
                {
                    Console.WriteLine("[ERROR] Argument Error");
                    Error_Message();
                    Environment.Exit(-1);
                }

                fileInPutPath = args[0];
                fileOutPutPath = args[1];

                fileOutPutName = Path.GetFileName(args[1]);

                //Check file input exist
                if (!File.Exists(fileInPutPath))
                {
                    Console.WriteLine("[ERROR] Not Found：{0}", fileInPutPath);
                    Environment.Exit(-1);
                }

                //Check file name output input
                if (!Path.GetExtension(fileInPutPath).ToLower().Contains("mid"))
                {
                    Console.WriteLine("[ERROR] File Input Is Not MID: {0}", fileInPutPath);
                    Environment.Exit(-1);
                }

                //Check file name output input
                if (string.IsNullOrWhiteSpace(Path.GetExtension(fileOutPutName)) || !Path.GetExtension(fileOutPutName).ToLower().Contains("mid"))
                {
                    Console.WriteLine("[ERROR] File Output Is Not MID: {0}", fileOutPutName);
                    Environment.Exit(-1);
                }

                //Check Empty path name is not legal.
                if (!Directory.Exists(Path.GetDirectoryName(fileOutPutPath)))
                {
                    fileOutPutPath = string.Format("{0}\\{1}", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileOutPutName);
                }

                if (MarkerUtils.MarkerRemoving(fileInPutPath, fileOutPutPath) != MIDI_ERROR_CODE.NO_ERROR)
                {
                    iResult = -1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                iResult = -1;
            }

            ExitOut(iResult);
        }

        /// <summary>
        /// 引数がおかしいときのエラーメッセージ表示用
        /// </summary>
        static void Error_Message()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Please confirm the arguments which you passed");
            Console.WriteLine("1st argument <<-- Input Midi file Path");
            Console.WriteLine("2nd argument <<-- Output Midi file Path");
            Console.ResetColor();
        }

        static void ExitOut(int iResult)
        {
            string message = string.Format("{0}:正常終了(OK)", iResult);
            if (iResult != 0)
            {
                message = string.Format("{0}:エラー終了(ERROR)", iResult);
            }
            Console.WriteLine(message);
            Environment.Exit(iResult);
        }
    }
}
