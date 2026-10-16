using UnityEngine;
using System;
using System.IO;

// Where Block Island keeps the player's files: Documents/Block Island/<Saves | Bauwerke | BlockTextures | CustomBlocks | Prefabs | Musik>.
// On macOS .NET reports the home folder as "MyDocuments", so the real Documents folder is used explicitly.
// Files from the former location (~/Block Island) are moved over once; nothing that already exists is overwritten.
public static class BlockIslandPaths {
    public const string Saves = "Saves", Structures = "Bauwerke", BlockTextures = "BlockTextures", CustomBlocks = "CustomBlocks", Prefabs = "Prefabs", Music = "Musik";

    static string root;

    public static string DocumentsFolder {
        get {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if(string.IsNullOrEmpty(docs)) return null;
            if(Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor) {
                string real = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents");
                if(Directory.Exists(real)) return real;
            }
            return docs;
        }
    }

    // The former location: "Block Island" next to what .NET calls MyDocuments (the home folder on macOS).
    public static string OldRoot {
        get {
            string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return string.IsNullOrEmpty(docs) ? null : Path.Combine(docs, "Block Island");
        }
    }

    public static string Root {
        get {
            if(root != null) return root;
            // Automated tests use their own data folder (environment variable), never the player's files.
            string over = Environment.GetEnvironmentVariable("BI_DATA_DIR");
            if(!string.IsNullOrEmpty(over)) { root = over; Directory.CreateDirectory(root); return root; }
            string docs = DocumentsFolder;
            root = string.IsNullOrEmpty(docs) ? Path.Combine(Application.persistentDataPath, "Block Island") : Path.Combine(docs, "Block Island");
            Directory.CreateDirectory(root);
            MoveFromOldLocation();
            return root;
        }
    }

    public static string Folder(string name) {
        string p = Path.Combine(Root, name);
        Directory.CreateDirectory(p);
        return p;
    }

    static void MoveFromOldLocation() {
        string old = OldRoot;
        if(string.IsNullOrEmpty(old) || !Directory.Exists(old)) return;
        if(string.Equals(Path.GetFullPath(old).TrimEnd('/', '\\'), Path.GetFullPath(root).TrimEnd('/', '\\'), StringComparison.OrdinalIgnoreCase)) return;
        int moved = 0, kept = 0;
        try {
            MoveContents(old, root, ref moved, ref kept);
            if(kept == 0) DeleteEmpty(old);
            Debug.Log("BLOCK ISLAND PATHS: moved " + moved + " entries from " + old + " to " + root + (kept > 0 ? " (" + kept + " left there because they already exist in the new place)" : ""));
        } catch(Exception e) { Debug.LogWarning("BLOCK ISLAND PATHS: could not move files from " + old + ": " + e.Message); }
    }

    static void MoveContents(string src, string dst, ref int moved, ref int kept) {
        Directory.CreateDirectory(dst);
        foreach(string f in Directory.GetFiles(src)) {
            string name = Path.GetFileName(f);
            if(name == ".DS_Store") { try { File.Delete(f); } catch { } continue; }
            string t = Path.Combine(dst, name);
            if(File.Exists(t) || Directory.Exists(t)) { kept++; continue; }
            File.Move(f, t); moved++;
        }
        foreach(string d in Directory.GetDirectories(src)) {
            string t = Path.Combine(dst, Path.GetFileName(d));
            if(!Directory.Exists(t) && !File.Exists(t)) { Directory.Move(d, t); moved++; }
            else MoveContents(d, t, ref moved, ref kept);
        }
    }

    static void DeleteEmpty(string dir) {
        foreach(string d in Directory.GetDirectories(dir)) DeleteEmpty(d);
        foreach(string f in Directory.GetFiles(dir)) if(Path.GetFileName(f) == ".DS_Store") File.Delete(f);
        if(Directory.GetFiles(dir).Length == 0 && Directory.GetDirectories(dir).Length == 0) Directory.Delete(dir);
    }
}
