using NAudio.Wave;
using System;
using System.IO;

namespace ClickyWindows;

public class AudioCaptureManager
{
    private WaveInEvent? _waveIn;
    private MemoryStream? _audioStream;
    private WaveFileWriter? _writer;

    public event EventHandler<byte[]>? AudioDataAvailable;

    public void StartRecording()
    {
        _waveIn = new WaveInEvent();
        _waveIn.WaveFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16-bit, mono
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _audioStream = new MemoryStream();
        _writer = new WaveFileWriter(_audioStream, _waveIn.WaveFormat);

        _waveIn.StartRecording();
    }

    public void StopRecording()
    {
        _waveIn?.StopRecording();
    }

    public void Dispose()
    {
        _writer?.Dispose();
        _audioStream?.Dispose();
        _waveIn?.Dispose();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        _writer?.Write(e.Buffer, 0, e.BytesRecorded);
        AudioDataAvailable?.Invoke(this, e.Buffer);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _writer?.Dispose();
        _writer = null;

        if (_audioStream != null)
        {
            var audioData = _audioStream.ToArray();
            // TODO: Process audio data (send to transcription)
            _audioStream.Dispose();
            _audioStream = null;
        }

        _waveIn?.Dispose();
        _waveIn = null;
    }

    public byte[] GetRecordedAudio()
    {
        return _audioStream?.ToArray() ?? Array.Empty<byte>();
    }
}