using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace VLTK.Production.World.Unity
{
    public sealed class ProductionMapRuntimeLoader
    {
        public string CatalogPath => Path.Combine(Application.streamingAssetsPath, MapRuntimeContract.CatalogPath);
        public string ArtifactPath => Path.Combine(Application.streamingAssetsPath, MapRuntimeContract.ArtifactPath);
        public string ProvenancePath => Path.Combine(Application.streamingAssetsPath, MapRuntimeContract.ProvenancePath);
        public string SignaturePath => Path.Combine(Application.streamingAssetsPath, MapRuntimeContract.SignaturePath);

        public MapRuntimeManifest Load(MapRuntimeTrustMode trustMode, out MapRuntimeValidationResult validation)
        {
            validation = new MapRuntimeValidationResult(false, "missing_files");
            if (!File.Exists(CatalogPath) || !File.Exists(ArtifactPath) || !File.Exists(ProvenancePath) || !File.Exists(SignaturePath))
                return null;
            byte[] catalogBytes = File.ReadAllBytes(CatalogPath);
            byte[] artifactBytes = File.ReadAllBytes(ArtifactPath);
            byte[] provenanceBytes = File.ReadAllBytes(ProvenancePath);
            byte[] signatureBytes = File.ReadAllBytes(SignaturePath);
            var catalog = JsonConvert.DeserializeObject<MapRuntimeCatalog>(System.Text.Encoding.UTF8.GetString(catalogBytes));
            var manifest = JsonConvert.DeserializeObject<MapRuntimeManifest>(System.Text.Encoding.UTF8.GetString(artifactBytes));
            var signature = JsonConvert.DeserializeObject<MapRuntimeSignatureFile>(System.Text.Encoding.UTF8.GetString(signatureBytes));
            validation = MapRuntimeValidator.Validate(
                catalog,
                manifest,
                signature,
                MapRuntimeValidator.Sha256Hex(artifactBytes),
                MapRuntimeValidator.Sha256Hex(provenanceBytes),
                MapRuntimeValidator.Sha256Hex(signatureBytes),
                trustMode);
            return validation.ok ? manifest : null;
        }
    }
}
