// <copyright file="UserDataTcpFrameIo.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net.Sockets;
using System.Text;

namespace CipherBank_app.UserData;

/// <summary>Shared CIPHERBANK_INTERNAL frame read helper (EOF-delimited).</summary>
public static class UserDataTcpFrameIo
{
    /// <summary>
    /// Reads UTF-8 until EOF marker; returns text without the marker.
    /// Use: High (TCP client/server). Scope: userdata transport.
    /// </summary>
    public static async Task<string> ReadUntilEofAsync(
        NetworkStream stream,
        string eof,
        int maxFrameBytes,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (string.IsNullOrEmpty(eof))
        {
            throw new ArgumentException("EOF marker is required.", nameof(eof));
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFrameBytes);

        byte[] buffer = new byte[4096];
        StringBuilder sb = new();
        int receivedBytes = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
            if (read == 0)
            {
                throw new IOException("TCP connection closed before userdata EOF.");
            }

            receivedBytes = checked(receivedBytes + read);
            if (receivedBytes > maxFrameBytes)
            {
                throw new InvalidDataException($"Userdata frame exceeds the {maxFrameBytes}-byte limit.");
            }

            sb.Append(Encoding.UTF8.GetString(buffer, 0, read));
            string text = sb.ToString();
            int idx = text.IndexOf(eof, StringComparison.Ordinal);
            if (idx >= 0)
            {
                return text[..idx];
            }
        }
    }
}
