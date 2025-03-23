// Decompiled with JetBrains decompiler
// Type: Oxide.Game.Rust.RustExtension
// Assembly: Oxide.Rust, Version=2.0.6410.0, Culture=neutral, PublicKeyToken=null
// MVID: 482C0C9C-1F97-4EF2-85ED-AB27B0E34159
// Assembly location: D:\rust_server\RustDedicated_Data\Managed\Oxide.Rust.dll

using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Libraries;
using Oxide.Plugins;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace Oxide.Game.Rust
{
    public class RustExtension : Extension
    {
        private const string OxideRustReleaseListUrl = "https://api.github.com/repos/OxideMod/Oxide.Rust/releases";
        internal static Assembly Assembly = Assembly.GetExecutingAssembly();
        internal static AssemblyName AssemblyName = RustExtension.Assembly.GetName();
        internal static VersionNumber AssemblyVersion = new VersionNumber(RustExtension.AssemblyName.Version.Major, RustExtension.AssemblyName.Version.Minor, RustExtension.AssemblyName.Version.Build);
        internal static string AssemblyAuthors = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(RustExtension.Assembly, typeof(AssemblyCompanyAttribute), false)).Company;
        private static readonly WebClient WebClient = new WebClient();
        private static VersionNumber LatestExtVersion = RustExtension.AssemblyVersion;
        public static string[] Filter = new string[18]
        {
      "alphamapResolution is clamped to the range of",
      "AngryAnt Behave version",
      "Floating point textures aren't supported on this device",
      "HDR RenderTexture format is not supported on this platform.",
      "Image Effects are not supported on this platform.",
      "Missing projectileID",
      "Motion vectors not supported on a platform that does not support",
      "The image effect Main Camera",
      "The image effect effect -",
      "Unable to find shaders",
      "Unsupported encoding: 'utf8'",
      "Warning, null renderer for ScaleRenderer!",
      "[AmplifyColor]",
      "[AmplifyOcclusion]",
      "[CoverageQueries] Disabled due to unsupported",
      "[CustomProbe]",
      "[Manifest] URI IS",
      "[SpawnHandler] populationCounts"
        };

        public override bool IsGameExtension => true;

        public override string Name => "Rust";

        public override string Author => RustExtension.AssemblyAuthors;

        public override VersionNumber Version => RustExtension.AssemblyVersion;

        public override string[] DefaultReferences => new string[35]
        {
      "0Harmony",
      "Facepunch.Network",
      "Facepunch.Steamworks.Posix64",
      "Facepunch.System",
      "Facepunch.UnityEngine",
      "Facepunch.Steamworks.Win64",
      "Rust.Data",
      "Rust.FileSystem",
      "Rust.Clans",
      "Rust.Clans.Local",
      "Rust.Global",
      "Rust.Localization",
      "Rust.Platform",
      "Rust.Platform.Common",
      "Rust.Platform.Steam",
      "Rust.Workshop",
      "Rust.World",
      "System.Drawing",
      "UnityEngine.AIModule",
      "UnityEngine.AssetBundleModule",
      "UnityEngine.CoreModule",
      "UnityEngine.GridModule",
      "UnityEngine.ImageConversionModule",
      "UnityEngine.PhysicsModule",
      "UnityEngine.TerrainModule",
      "UnityEngine.TerrainPhysicsModule",
      "UnityEngine.UI",
      "UnityEngine.UIModule",
      "UnityEngine.UIElementsModule",
      "UnityEngine.UnityWebRequestAudioModule",
      "UnityEngine.UnityWebRequestModule",
      "UnityEngine.UnityWebRequestTextureModule",
      "UnityEngine.UnityWebRequestWWWModule",
      "UnityEngine.VehiclesModule",
      "netstandard"
        };

        public override string[] WhitelistAssemblies => new string[20]
        {
      "Assembly-CSharp",
      "Assembly-CSharp-firstpass",
      "DestMath",
      "Facepunch.Network",
      "Facepunch.System",
      "Facepunch.UnityEngine",
      "mscorlib",
      "Oxide.Core",
      "Oxide.Rust",
      "RustBuild",
      "Rust.Data",
      "Rust.FileSystem",
      "Rust.Global",
      "Rust.Localization",
      "Rust.Localization",
      "Rust.Platform.Common",
      "Rust.Platform.Steam",
      "System",
      "System.Core",
      "UnityEngine"
        };

        public override string[] WhitelistNamespaces => new string[14]
        {
      "ConVar",
      "Dest",
      "Facepunch",
      "Network",
      "Oxide.Game.Rust.Cui",
      "ProtoBuf",
      "PVT",
      "Rust",
      "Steamworks",
      "System.Collections",
      "System.Security.Cryptography",
      "System.Text",
      "System.Threading.Monitor",
      "UnityEngine"
        };

        public RustExtension(ExtensionManager manager)
          : base(manager)
        {
        }

        public override void Load()
        {
            this.Manager.RegisterLibrary("Rust", (Library)new Oxide.Game.Rust.Libraries.Rust());
            this.Manager.RegisterLibrary("Command", (Library)new Command());
            this.Manager.RegisterLibrary("Item", (Library)new Item());
            this.Manager.RegisterLibrary("Player", (Library)new Player());
            this.Manager.RegisterLibrary("Server", (Library)new Server());
            this.Manager.RegisterPluginLoader((PluginLoader)new RustPluginLoader());
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                Cleanup.Add("Facepunch.Steamworks.Win64.dll");
            RustExtension.WebClient.Headers["User-Agent"] = string.Format("Oxide.Rust {0}", (object)this.Version);
        }

        public override void LoadPluginWatchers(string directory)
        {
        }

        public override void OnModLoad() => CSharpPluginLoader.PluginReferences.UnionWith((IEnumerable<string>)this.DefaultReferences);

        public void GetLatestVersion(Action<VersionNumber, Exception> callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback), "Callback cannot be null");
            if (RustExtension.LatestExtVersion > RustExtension.AssemblyVersion)
                callback(RustExtension.LatestExtVersion, (Exception)null);
            else
                this.GetLatestExtensionVersion().ContinueWith((Action<Task<VersionNumber>>)(task =>
                {
                    if (task.Exception == null)
                        RustExtension.LatestExtVersion = task.Result;
                    callback(RustExtension.LatestExtVersion, task.Exception?.InnerException);
                }));
        }

        private async Task<VersionNumber> GetLatestExtensionVersion()
        {
            string jsonString = await RustExtension.WebClient.DownloadStringTaskAsync("https://api.github.com/repos/OxideMod/Oxide.Rust/releases");
            string versionString = !string.IsNullOrWhiteSpace(jsonString) ? JSON.Array.Parse(jsonString)[0].Obj.GetString("tag_name") : throw new Exception("Could not retrieve latest Oxide.Rust version from GitHub API");
            return !string.IsNullOrWhiteSpace(versionString) ? this.ParseVersionNumber(versionString) : throw new Exception("Tag name is undefined");
        }

        private VersionNumber ParseVersionNumber(string versionString)
        {
            string[] strArray = versionString.Split(new char[1]
            {
        '.'
            }, StringSplitOptions.RemoveEmptyEntries);
            return new VersionNumber(int.Parse(strArray[0]), int.Parse(strArray[1]), int.Parse(strArray[2]));
        }
    }
}
