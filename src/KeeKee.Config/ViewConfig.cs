// Copyright 2025 Robert Adams
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


namespace KeeKee.Config {

    public class ViewConfig {
        public static string subSectionName { get; set; } = "View";

        public class ViewCameraInfo {
            // Units per second to move camera
            public float Speed { get; set; } = 5.0f;
            // Degrees to rotate camera
            public float RotationSpeed { get; set; } = 0.100f;
            // Far distance sent to server
            public float ServerFar { get; set; } = 300.0f;
            // Distance camera is behind agent
            public float BehindAgent { get; set; } = 4.0f;
            // Distance camera is above agent (combined with behind)
            public float AboveAgent { get; set; } = 2.0f;
        };

        public ViewCameraInfo Camera { get; set; } = new ViewCameraInfo();

        public int MaxAgents { get; set; } = 100;
        public int MaxObjects { get; set; } = 1000;
        public int MaxParticles { get; set; } = 1000;
        public int MaxTextures { get; set; } = 500;
        public int MaxTextureMemoryMB { get; set; } = 256;
        public int MaxTextureSize { get; set; } = 512;
        public bool UseTextureCompression { get; set; } = true;
        public bool AnisotropicTextures { get; set; } = true;
        public int AnisotropicLevel { get; set; } = 4;
        public bool MipMappedTextures { get; set; } = true;
        public bool HighResTextures { get; set; } = false;
        public bool LoadAllAvatarTextures { get; set; } = false;
        public bool ShowAvatars { get; set; } = true;
        public bool ShowObjects { get; set; } = true;
        public bool ShowParticles { get; set; } = true;
        public bool ShowSky { get; set; } = true;
        public bool ShowWater { get; set; } = true;
        public bool ShowGround { get; set; } = true;
        public bool ShowClouds { get; set; } = true;
        public bool ShowSunMoon { get; set; } = true;
        public bool ShowDebugInfo { get; set; } = false;
        public bool ShowUserInterface { get; set; } = true;
        public bool WireFrameMode { get; set; } = false;
        public bool BackFaceCulling { get; set; } = true;
        public bool FogEnabled { get; set; } = true;
        public float FogDensity { get; set; } = 0.001f;
        public string FogColor { get; set; } = "<0.5,0.5,0.5>";
        public bool LightingEnabled { get; set; } = true;
        public bool SmoothLighting { get; set; } = true;
        public bool DynamicShadows { get; set; } = true;
        public int ShadowMapSize { get; set; } = 1024;
        public float ShadowDistance { get; set; } = 32.0f;
        public bool AvatarShadows { get; set; } = true;
        public bool ObjectShadows { get; set; } = true;
        public bool ParticleShadows { get; set; } = false;
        public int FramesPerSecond { get; set; } = 30;

    }
}

