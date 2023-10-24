using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace Galaxy_Widgets_Background_Service
{
    public class SongInfo
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Duration { get; set; }
        public string Position { get; set; }
        public double PositionPercent { get; set; }
        public string CoverUrl { get; set; }

    }

    class Program
    {
        static string formatSecondsToTime(double totalSeconds)
        {
            double minutes = totalSeconds / 60;
            double seconds = totalSeconds % 60;


            // Use string interpolation to format the time as "mm:ss"
            return Math.Floor(minutes) + ":" + Math.Floor(seconds).ToString("00");
        }

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

                var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Galaxy-Widgets\\temp\\deviceCareInfo.json";

                File.WriteAllText(path, jsonString);

                await Task.Delay(1000 * 30);
            }

        }



        async static Task writeMusicJson()
        {
            double lastPositionSeconds = 0; // Store the last known position
            long lastTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            double estimatedPositionSeconds = 0.0;
            string lastTitle = ""; // Store the last known title

            while (true)
            {
                await Task.Delay(800);
                try
                {
                    var sessionManager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult();
                    // Get the currently active session
                    var session = sessionManager.GetCurrentSession();

                    var songInfo = new SongInfo
                    {
                        Title = "",
                        Artist = "",
                        Duration = "0:00",
                        Position = "0:00",
                        PositionPercent = 0.0, // Initialize as double
                        CoverUrl = "",
                    };

                    if (session != null)
                    {
                        var mediaProperties = session.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
                        var playbackInfo = session.GetPlaybackInfo();
                        var timeLineProperties = session.GetTimelineProperties();

                        if (mediaProperties != null)
                        {
                            string currentTitle = mediaProperties.Title;

                            if (currentTitle != lastTitle)
                            {
                                // Title has changed, reset estimatedPositionSeconds to 0
                                estimatedPositionSeconds = 0.0;
                                lastTitle = currentTitle;
                            }

                            songInfo = new SongInfo
                            {
                                Title = currentTitle,
                                Artist = mediaProperties.Artist,
                                Duration = formatSecondsToTime(timeLineProperties.EndTime.TotalSeconds),
                                CoverUrl = "",
                            };

                            // Calculate the estimated position percent based on the time elapsed since the last update
                            long currentTimestamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                            long elapsedMilliseconds = currentTimestamp - lastTimestamp;
                            lastTimestamp = currentTimestamp;
                            double currentPositionSeconds = timeLineProperties.Position.TotalSeconds;

                            // Calculate the estimated position in seconds
                            if (Math.Floor(estimatedPositionSeconds) == currentPositionSeconds || estimatedPositionSeconds > currentPositionSeconds)
                            {
                                estimatedPositionSeconds = estimatedPositionSeconds + (elapsedMilliseconds / 1000.0);
                            }
                            else if (estimatedPositionSeconds < currentPositionSeconds)
                            {
                                estimatedPositionSeconds = currentPositionSeconds + (elapsedMilliseconds / 1000.0);
                            }
                            else if (estimatedPositionSeconds > timeLineProperties.EndTime.TotalSeconds)
                            {
                                estimatedPositionSeconds = timeLineProperties.EndTime.TotalSeconds;
                            }


                            if (playbackInfo.PlaybackStatus.ToString() != "Playing")
                            {
                                estimatedPositionSeconds = currentPositionSeconds;
                            }

                            var thumbnail = mediaProperties?.Thumbnail;
                            if (thumbnail != null)
                            {
                                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Galaxy-Widgets\\temp";
                                string filePath = Path.Combine(folderPath, "cover.jpg");

                                // Open the stream for the thumbnail
                                var stream = await thumbnail.OpenReadAsync();

                                // Read the stream into a byte array
                                var bytes = new byte[stream.Size];
                                var reader = new DataReader(stream.GetInputStreamAt(0));
                                await reader.LoadAsync((uint)stream.Size);
                                reader.ReadBytes(bytes);

                                // Write the byte array to the file
                                File.WriteAllBytes(filePath, bytes);

                                // Set the cover URL to the file path
                                songInfo.CoverUrl = filePath;
                            }

                            songInfo.Position = formatSecondsToTime(estimatedPositionSeconds);
                            songInfo.PositionPercent = Math.Floor(estimatedPositionSeconds / timeLineProperties.EndTime.TotalSeconds * 100);

                            // Update the last known position
                            lastPositionSeconds = estimatedPositionSeconds;
                        }
                    }

                    string jsonString = JsonSerializer.Serialize(songInfo, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    var jsonPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Galaxy-Widgets\\temp\\songInfo.json";

                    File.WriteAllText(jsonPath, jsonString);

                }
                catch { }
            }
        }

        async static Task Main()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            if (!Directory.Exists(path + "\\Galaxy-Widgets\\temp")) Directory.CreateDirectory(path + "\\Galaxy-Widgets\\temp");

            var taskMusic = writeMusicJson();
            var taskDeviceCare = writeDeviceCareJson();

            await Task.WhenAll(taskMusic, taskDeviceCare);
        }
    }
}
