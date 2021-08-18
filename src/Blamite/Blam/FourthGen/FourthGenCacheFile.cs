using System.Collections.Generic;
using System.Linq;
using Blamite.Blam;
using Blamite.Blam.Localization;
using Blamite.Blam.Resources.Sounds;
using Blamite.Blam.Scripting;
using Blamite.Blam.Shaders;
using Blamite.Blam.FourthGen.Localization;
using Blamite.Blam.FourthGen.Resources;
using Blamite.Blam.FourthGen.Shaders;
using Blamite.Blam.FourthGen.Structures;
using Blamite.Blam.Util;
using Blamite.Serialization;
using Blamite.IO;
using Blamite.Util;

namespace Blamite.Blam.FourthGen
{
    /// <summary>
    ///     A Forth-Generation Blam (map/dat) cache file.
    /// </summary>
    public class FourthGenCacheFile
    {
        private readonly EngineDescription _buildInfo;
        private readonly FileSegmenter _segmenter;
        private IndexedFileNameSource _fileNames;
        private FourthGenHeader _header;
        private FourthGenLanguageGlobals _languageInfo;
        private FourthGenLanguagePackLoader _languageLoader;
        private FourthGenResourceMetaLoader _resourceMetaLoader;
        private IResourceManager _resources;
        private IndexedStringIDSource _stringIds;
        private FourthGenTagTable _tags;
        private FourthGenSimulationDefinitionTable _simulationDefinitions;
        private FourthGenPointerExpander _expander;
        private Endian _endianness;
        private EffectInterop _effects;
        private SoundResourceManager _soundGestalt;
        
        private bool _zoneOnly = false;

        public FourthGenCacheFile(IReader reader, EngineDescription buildInfo, string filePath)
        {
            FilePath = filePath;
            _endianness = reader.Endianness;
            _buildInfo = buildInfo;
            _segmenter = FileSegmenter(buildInfo.SegmentAlignment);
            _expander = FourthGenPointerExpander(buildInfo.ExpandMagic);

            Load(reader);
        }

        public FourthGenHeader FullHeader
        {
            get { return _header; }
        }

        public void SaveChanges(Istream stream)
        {
            _tags.SaveChanges(stream);
        }
    }
}