namespace nobudgetSampler;
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualBasic;
using NAudio.Wave;
class Program
{
    static Dictionary<char, string> samplePaths;
    static Dictionary<char, WaveOutEvent> waveOutEvents;
    static Dictionary<char, double> sampleLengths;
    static WaveOutEvent stopWaveOutEvent;
    static Thread displayThread;
    static bool isPlaying;


    static void Main(string[] args)
    {

        Console.BackgroundColor = ConsoleColor.DarkRed;
        InitializeSamples();

        displayThread = new Thread(DisplayThreadMethod);
        displayThread.Start();

        ConsoleKeyInfo keyInfo;
        do
        {
            keyInfo = Console.ReadKey();
            if (keyInfo.Key == ConsoleKey.S)
            {
                StopAllPlayback();
            }
            else
            {
                PlaySample(keyInfo.KeyChar);
            }
        } while (keyInfo.Key != ConsoleKey.Escape);

        CleanupSamples();

        // Stop the display thread
        displayThread.Join();
    }

    static void InitializeSamples()
    {
        samplePaths = new Dictionary<char, string>
        {
            { 'q', "C:\\Users\\cinek\\source\\repos\\wowo\\wowo\\sample\\chimes.wav" },
            { 'w', "C:\\Users\\cinek\\source\\repos\\wowo\\wowo\\sample\\hihat.wav" },
            { 'e', "C:\\Users\\cinek\\source\\repos\\wowo\\wowo\\sample\\kick.wav" },
            { 'r', "C:\\Users\\cinek\\source\\repos\\wowo\\wowo\\sample\\snare.wav" }
        };

        waveOutEvents = new Dictionary<char, WaveOutEvent>();
        sampleLengths = new Dictionary<char, double>();
        isPlaying = false;

        foreach (var kvp in samplePaths)
        {
            char key = kvp.Key;
            string filePath = kvp.Value;

            using (var audioFileReader = new AudioFileReader(filePath))
            {
                sampleLengths[key] = audioFileReader.TotalTime.TotalSeconds;
            }
        }
    }

    static void CleanupSamples()
    {
        foreach (var waveOutEvent in waveOutEvents.Values)
        {
            waveOutEvent?.Dispose();
        }
    }

    static void PlaySample(char key)
    {
        if (samplePaths.ContainsKey(key))
        {
            string filePath = samplePaths[key];
            WaveOutEvent waveOutEvent;
            if (waveOutEvents.ContainsKey(key))
            {
                waveOutEvent = waveOutEvents[key];
            }
            else
            {
                waveOutEvent = new WaveOutEvent();
                waveOutEvents[key] = waveOutEvent;
            }

            var audioFileReader = new AudioFileReader(filePath);
            audioFileReader.Position = 0;

            waveOutEvent.Stop();
            waveOutEvent.Init(audioFileReader);
            waveOutEvent.Play();
        }
        else
        {
            Console.WriteLine("Invalid key.");
        }
    }
    static void StopAllPlayback()
    {
        if (stopWaveOutEvent == null)
        {
            stopWaveOutEvent = new WaveOutEvent();
            stopWaveOutEvent.Init(new SilenceProvider(new WaveFormat(44100, 2)));
            stopWaveOutEvent.Play();
        }
        else
        {
            stopWaveOutEvent.Stop();
            stopWaveOutEvent.Dispose();
            stopWaveOutEvent = null;
        }

        foreach (var waveOutEvent in waveOutEvents.Values)
        {
            waveOutEvent?.Stop();
        }

    }
    static void DisplayThreadMethod()
    {
        while (true)
        {
            Console.Clear();
            Console.SetWindowSize(69, 60);
            StartText();
            foreach (var kvp in waveOutEvents)
            {
                var key = kvp.Key;
                var waveOutEvent = kvp.Value;
                var isCurrentlyPlaying = IsPlaying(key);

                if (samplePaths.TryGetValue(key, out var filePath))
                {
                    var sampleLength = GetSampleLength(filePath);
                    double sampleRate = waveOutEvent.OutputWaveFormat.SampleRate;
                    int channels = waveOutEvent.OutputWaveFormat.Channels;

                    if (waveOutEvent != null && waveOutEvent.PlaybackState != PlaybackState.Stopped)
                    {
                        var position = waveOutEvent.GetPosition();
                        var progress = (double)position / (sampleRate * channels * (sampleLength?.TotalMilliseconds ?? 0) / 1000);
                        progress = progress / 4;
                        Console.WriteLine($"[{key}] {(isCurrentlyPlaying ? "Playing" : "Stopped")}: {progress:P0}" + " " + CreateProgressBar((float)progress, (float)(sampleLength?.TotalMilliseconds ?? 0) / 1000), Console.ForegroundColor = ConsoleColor.Green);
                    }
                    else
                    {
                        Console.WriteLine($"[{key}] {(isCurrentlyPlaying ? "Playing" : "Stopped")}: Audio playback stopped", Console.ForegroundColor = ConsoleColor.Blue);
                    }
                }
                else
                {
                    Console.WriteLine($"[{key}] {(isCurrentlyPlaying ? "Playing" : "Stopped")}: Sample not found", Console.ForegroundColor = ConsoleColor.DarkRed);
                }

            }

            Thread.Sleep(25); // Update every 100 milliseconds
        }
    }

    static TimeSpan? GetSampleLength(string filePath)
    {
        try
        {
            using (var audioFile = new AudioFileReader(filePath))
            {
                return audioFile.TotalTime;
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    static string CreateProgressBar(float value, float sampleLength)
    {
        const int progressBarWidth = 35;
        float clampedValue = Math.Clamp(value, 0f, 1f);
        int progressWidth = (int)(clampedValue * progressBarWidth);
        int sampleProgress = (int)(clampedValue * sampleLength);
        sampleProgress = Math.Min(sampleProgress, (int)sampleLength);
        string progressBar = "[" + new string('#', progressWidth) + new string('-', progressBarWidth - progressWidth) + "]";
        return $"{progressBar} ({sampleProgress}/{sampleLength})";
    }

    static bool IsPlaying(char key)
    {
        if (waveOutEvents.ContainsKey(key))
        {
            var waveOutEvent = waveOutEvents[key];
            return waveOutEvent.PlaybackState == PlaybackState.Playing;
        }
        return false;
    }

    static void StartText()
    {
        Console.WriteLine("                 --  nobudgetSampler v0.01  --", Console.ForegroundColor = ConsoleColor.White);
        Console.WriteLine("");
        Console.WriteLine("                     +---+---+---+---+");
        Console.WriteLine("                     | 1 | 2 | 3 | 4 |");
        Console.WriteLine("                     +---+---+---+---+");
        Console.WriteLine("                     | q | w | e | r |");
        Console.WriteLine("                     +---+---+---+---+");
        Console.WriteLine("                     | a | s | d | f |");
        Console.WriteLine("                     +---+---+---+---+");
        Console.WriteLine("                     | z | x | c | v |");
        Console.WriteLine("                     +---+---+---+---+");
        Console.WriteLine("");
        Console.WriteLine("+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+---+");

    }

}
