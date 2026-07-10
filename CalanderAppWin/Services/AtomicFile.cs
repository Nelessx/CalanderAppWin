using System;
using System.IO;
using System.Text;

namespace NepaliCalendar.App.Services
{
    /// <summary>
    /// Crash-safe file persistence. A raw <see cref="File.WriteAllText(string, string)"/> that is
    /// interrupted mid-write (power loss, crash, forced kill) leaves the only copy truncated and
    /// unparseable — permanent, silent data loss. These helpers write to a temporary sibling file
    /// first and then atomically swap it into place, keeping the previous good copy as a
    /// <c>.bak</c> so a torn write can always be recovered.
    /// </summary>
    public static class AtomicFile
    {
        /// <summary>
        /// Writes <paramref name="content"/> to <paramref name="path"/> atomically. On any file
        /// system that supports it, the destination is replaced in a single operation and the
        /// prior contents are preserved in <paramref name="path"/> + ".bak".
        /// </summary>
        public static void WriteAllText(string path, string content)
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";

            // Write + flush the full payload to a temp file before touching the real one.
            File.WriteAllText(tempPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            if (File.Exists(path))
            {
                // Atomic on NTFS: the destination is only ever the old file or the new file,
                // never a half-written one. The old contents survive in the .bak.
                File.Replace(tempPath, path, backupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }

        /// <summary>
        /// Reads text from <paramref name="path"/>, transparently falling back to the <c>.bak</c>
        /// copy if the primary file is missing or unreadable. Returns null if neither can be read.
        /// </summary>
        public static string? ReadAllTextWithRecovery(string path)
        {
            try
            {
                if (File.Exists(path))
                    return File.ReadAllText(path);
            }
            catch
            {
                // fall through to backup
            }

            string backupPath = path + ".bak";
            try
            {
                if (File.Exists(backupPath))
                    return File.ReadAllText(backupPath);
            }
            catch
            {
                // give up
            }

            return null;
        }
    }
}
