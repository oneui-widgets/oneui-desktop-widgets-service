using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Text.Json;
using WNPReduxAdapterLibrary;

namespace oneui_desktop_widgets_service
{

    class Program
    {
        async static Task writeDeviceCareJson()
        {
            while (true)
            {
                DriveInfo driveInfo = new DriveInfo("C");


                long totalSizeBytes = driveInfo.TotalSize;
                long usedSpaceBytes = driveInfo.TotalSize - driveInfo.TotalFreeSpace;

                var deviceCareInfo = new
                {
                    Existing = totalSizeBytes,
                    Used = usedSpaceBytes
                };

                string jsonString = JsonSerializer.Serialize(deviceCareInfo, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\OneUI-Widgets\\temp\\deviceCareInfo.json";

                File.WriteAllText(path, jsonString);

                await Task.Delay(1000 * 30);
            }

        }


        async static Task writeMusicJson()
        {
            while (true)
            {
                void logger(LogType type, string message)
                {
                    Console.WriteLine($"{type}: {message}");
                }

                WNPRedux.Start(8943, "1.0.0", logger);

                var songInfo = new
                {
                    Title = WNPRedux.MediaInfo.Title,
                    Artist = WNPRedux.MediaInfo.Artist,
                    Duration = WNPRedux.MediaInfo.Duration,
                    Position = WNPRedux.MediaInfo.Position,
                    PositionPercent = WNPRedux.MediaInfo.PositionPercent,
                    CoverUrl = WNPRedux.MediaInfo.CoverUrl
                };

                string jsonString = JsonSerializer.Serialize(songInfo, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\OneUI-Widgets\\temp\\songInfo.json";

                File.WriteAllText(path, jsonString);

                await Task.Delay(1000);
            }
        }

        async static Task Main()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            if (!Directory.Exists(path + "\\OneUI-Widgets\\temp")) Directory.CreateDirectory(path + "\\OneUI-Widgets\\temp");

            var taskMusic = writeMusicJson();
            var taskDeviceCare = writeDeviceCareJson();

            await Task.WhenAll(taskMusic, taskDeviceCare);
        }
    }
}
