using Windows.Media.Control;
using Windows.Storage.Streams;

var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

var sessions = manager.GetSessions();
Console.WriteLine($"sessions: {sessions.Count}");

var current = manager.GetCurrentSession();
Console.WriteLine($"current : {current?.SourceAppUserModelId ?? "(none)"}");
Console.WriteLine();

foreach (var session in sessions)
{
    Console.WriteLine($"--- {session.SourceAppUserModelId}");

    try
    {
        var playback = session.GetPlaybackInfo();
        Console.WriteLine($"    status   : {playback?.PlaybackStatus}");
        Console.WriteLine($"    controls : play={playback?.Controls.IsPlayEnabled} pause={playback?.Controls.IsPauseEnabled} " +
                          $"next={playback?.Controls.IsNextEnabled} prev={playback?.Controls.IsPreviousEnabled} " +
                          $"seek={playback?.Controls.IsPlaybackPositionEnabled}");

        var props = await session.TryGetMediaPropertiesAsync();
        Console.WriteLine($"    title    : '{props?.Title}'");
        Console.WriteLine($"    artist   : '{props?.Artist}'");
        Console.WriteLine($"    album    : '{props?.AlbumTitle}'");
        Console.WriteLine($"    thumb    : {(props?.Thumbnail is null ? "none" : "present")}");

        // Actually pull the bytes down, which is what the app does when it renders artwork.
        if (props?.Thumbnail is IRandomAccessStreamReference reference)
        {
            try
            {
                using var stream = await reference.OpenReadAsync();
                Console.WriteLine($"    stream   : {stream.Size} bytes, contentType='{stream.ContentType}'");

                var buffer = new Windows.Storage.Streams.Buffer((uint)stream.Size);
                await stream.ReadAsync(buffer, (uint)stream.Size, InputStreamOptions.None);

                var bytes = new byte[buffer.Length];
                using (var reader = DataReader.FromBuffer(buffer))
                {
                    reader.ReadBytes(bytes);
                }

                var outPath = Path.Combine(Path.GetTempPath(), "smtc-thumb.bin");
                File.WriteAllBytes(outPath, bytes);
                Console.WriteLine($"    saved    : {outPath} ({bytes.Length} bytes)");
                Console.WriteLine($"    magic    : {BitConverter.ToString(bytes.Take(8).ToArray())}");

                // The app opens the same reference a second time for the colour wash.
                try
                {
                    using var second = await reference.OpenReadAsync();
                    Console.WriteLine($"    2nd open : OK, {second.Size} bytes");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    2nd open FAILED: {ex.GetType().FullName}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    THUMB READ FAILED: {ex.GetType().FullName}: {ex.Message}");
            }
        }

        var timeline = session.GetTimelineProperties();
        Console.WriteLine($"    timeline : pos={timeline.Position} start={timeline.StartTime} end={timeline.EndTime} updated={timeline.LastUpdatedTime}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"    FAILED: {ex.GetType().Name}: {ex.Message}");
    }

    Console.WriteLine();
}
