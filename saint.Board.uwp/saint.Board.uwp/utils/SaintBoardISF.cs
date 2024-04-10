using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace saint.Board.uwp.utils
{
    internal class SaintBoardISF
    {
        const string _extension = ".sbisf";
        string _name;
        string _savepath;
        StorageFile _file;

        static public string ExtensionName {  get { return _extension; } }
        public string Name{ get { return _name; } set { _name = value; } }
        public string SavePath { get { return _savepath; } set { _savepath = value; } }
        public StorageFile File { get { return _file; } set { _file = value; } }
        public bool IsValid { get { return _file != null; } }

        public SaintBoardISF (StorageFile file)
        {
            _file = file;
            _name = Path.GetFileNameWithoutExtension(file.Path);
            _savepath = file.Path;
        }

        public SaintBoardISF()
        {
            _file = null;
            _name = "Untitled";
            _savepath = String.Empty;
        }

    }
}
