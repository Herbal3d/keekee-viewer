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

using Microsoft.Extensions.Options;

using KeeKee.Config;
using KeeKee.Contexts;
using KeeKee.Entity;
using KeeKee.Framework.Logging;

using OMV = OpenMetaverse;

namespace KeeKee.World.LL {

    public class LLCmptAgentMovement : ICmptAgentMovement {
        private IKLogger m_log;

        public IEntity ContainingEntity { get; private set; }

        private OMV.GridClient m_client;
        private IOptions<LLAgentConfig> m_config;

        // if 'true', move avatar when we get the outgoing command to move the agent
        private bool m_shouldPreMoveAvatar = true;
        private float m_rotFudge = 1f;        // degrees moved per rotation
        private float m_moveFudge = 0.4f;     // meters moved per movement
        private float m_flyFudge = 2.5f;      // meters moved per movement
        private float m_runFudge = 0.8f;      // meters moved per movement

        public LLCmptAgentMovement(IKLogger pLog,
                        IEntity pContainingEntity,
                        OMV.GridClient pClient,
                        IOptions<LLAgentConfig> pConfig) {
            m_log = pLog;
            ContainingEntity = pContainingEntity;
            m_client = pClient;
            m_config = pConfig;

            m_shouldPreMoveAvatar = pConfig.Value.PreMoveAvatar;
            m_rotFudge = pConfig.Value.PreMoveRotFudge;
            m_moveFudge = pConfig.Value.PreMoveFudge;
            m_flyFudge = pConfig.Value.PreMoveFlyFudge;
            m_runFudge = pConfig.Value.PreMoveRunFudge;
        }

        // The underlying data has been updated. Forget local things.
        public void DataUpdate(UpdateCodes what) {
            // Local values are set for dead-reconning but once we have official values, use  them
            // if ((what & UpdateCodes.Position) != 0) m_haveLocalPosition = false;
            // if ((what & UpdateCodes.Rotation) != 0) m_haveLocalHeading = false;
        }

        public void StopAllMovement() {
            m_client.Self.Movement.Stop = true;
        }

        public void MoveForward(bool startstop) {
            m_client.Self.Movement.AtPos = startstop;
            m_client.Self.Movement.SendUpdate();
            // TODO: test if running or flying and use other fudges
            if (startstop && m_shouldPreMoveAvatar) {
                ICmptLocation avatarLocation = ContainingEntity.Cmpt<ICmptLocation>();
                OMV.Vector3 newPos = avatarLocation.LocalPosition +
                                new OMV.Vector3(CalcMoveFudge(), 0f, 0f) * avatarLocation.Heading;
                m_log.Log(KLogLevel.DWORLDDETAIL | KLogLevel.DUPDATEDETAIL, "MoveForward: premove from {0} to {1}",
                        avatarLocation.LocalPosition.ToString(), newPos);
                avatarLocation.LocalPosition = newPos;
                m_client.Self.RelativePosition = newPos;
                avatarLocation.LocalPosition = newPos;
                ContainingEntity.Update(UpdateCodes.Position);
            }
        }

        public void MoveBackward(bool startstop) {
            m_client.Self.Movement.AtNeg = startstop;
            m_client.Self.Movement.SendUpdate();
            if (startstop && m_shouldPreMoveAvatar) {
                ICmptLocation avatarLocation = ContainingEntity.Cmpt<ICmptLocation>();
                OMV.Vector3 newPos = avatarLocation.LocalPosition +
                            new OMV.Vector3(-CalcMoveFudge(), 0f, 0f) * avatarLocation.Heading;
                m_log.Log(KLogLevel.DWORLDDETAIL | KLogLevel.DUPDATEDETAIL, "MoveBackward: premove from {0} to {1}",
                        avatarLocation.LocalPosition.ToString(), newPos);
                avatarLocation.LocalPosition = newPos;
                avatarLocation.LocalPosition = newPos;
                m_client.Self.RelativePosition = newPos;
                ContainingEntity.Update(UpdateCodes.Position);
            }
        }

        public void MoveUp(bool startstop) {
            m_client.Self.Movement.UpPos = startstop;
            m_client.Self.Movement.SendUpdate();
            if (startstop && m_shouldPreMoveAvatar) {
                ICmptLocation avatarLocation = ContainingEntity.Cmpt<ICmptLocation>();
                avatarLocation.LocalPosition = avatarLocation.LocalPosition + new OMV.Vector3(0f, 0f, CalcMoveFudge());
                m_client.Self.RelativePosition = avatarLocation.LocalPosition;
                ContainingEntity.Update(UpdateCodes.Position);
            }
        }

        public void MoveDown(bool startstop) {
            m_client.Self.Movement.UpNeg = startstop;
            m_client.Self.Movement.SendUpdate();
            if (startstop && m_shouldPreMoveAvatar) {
                ICmptLocation avatarLocation = ContainingEntity.Cmpt<ICmptLocation>();
                avatarLocation.LocalPosition = avatarLocation.LocalPosition + new OMV.Vector3(0f, 0f, -CalcMoveFudge());
                m_client.Self.RelativePosition = avatarLocation.LocalPosition;
                ContainingEntity.Update(UpdateCodes.Position);
            }
        }

        public void Fly(bool startstop) {
            if (startstop) {
                // flying is modal. If we're flying, stop.
                m_client.Self.Movement.Fly = !m_client.Self.Movement.Fly;
                m_client.Self.Movement.SendUpdate();
            }
        }

        public void TurnLeft(bool startstop) {
            m_client.Self.Movement.TurnLeft = startstop;
            if (startstop) {
                OMV.Quaternion Zturn = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, Constants.PI / 180 * m_rotFudge);
                Zturn.Normalize();
                m_client.Self.Movement.BodyRotation = OMV.Quaternion.Normalize(m_client.Self.Movement.BodyRotation * Zturn);
                m_client.Self.Movement.HeadRotation = OMV.Quaternion.Normalize(m_client.Self.Movement.HeadRotation * Zturn);
                //m_client.Self.LocalPosition += Zturn;
            }
            m_client.Self.Movement.SendUpdate();
            if (startstop && m_shouldPreMoveAvatar) {
                ICmptLocation avatarLocation = ContainingEntity.Cmpt<ICmptLocation>();
                avatarLocation.Heading = m_client.Self.Movement.BodyRotation;
                avatarLocation.Heading = m_client.Self.Movement.BodyRotation;
                ContainingEntity.Update(UpdateCodes.Rotation);
                m_log.Log(KLogLevel.DWORLDDETAIL | KLogLevel.DUPDATEDETAIL, "TurnLeft: premove to {0}",
                        m_client.Self.Movement.BodyRotation);
            }
        }

        public void TurnRight(bool startstop) {
            m_client.Self.Movement.TurnRight = startstop;
            if (startstop) {
                OMV.Quaternion Zturn = OMV.Quaternion.CreateFromAxisAngle(OMV.Vector3.UnitZ, -Constants.PI / 180 * m_rotFudge);
                Zturn.Normalize();
                m_client.Self.Movement.BodyRotation = OMV.Quaternion.Normalize(m_client.Self.Movement.BodyRotation * Zturn);
                m_client.Self.Movement.HeadRotation = OMV.Quaternion.Normalize(m_client.Self.Movement.HeadRotation * Zturn);
                // m_client.Self.LocalPosition += Zturn;
            }
            // Send the movement (the turn) to the simulator. The rotation above will be corrected by the simulator
            m_client.Self.Movement.SendUpdate();
            // if we are to move the avatar when the user commands movement, push the avatar
            if (startstop && m_shouldPreMoveAvatar) {
                ICmptLocation avatarLocation = ContainingEntity.Cmpt<ICmptLocation>();
                avatarLocation.Heading = m_client.Self.Movement.BodyRotation;
                m_log.Log(KLogLevel.DWORLDDETAIL | KLogLevel.DUPDATEDETAIL, "TurnRight: premove to {0}",
                    m_client.Self.Movement.BodyRotation);
                // This next call sets off a tricky calling sequence:
                // LLEntityAvatar.Update
                //    calls LLEntityBase.Update
                //        calls EntityBase.Update
                //            (entity updates percolate up to the entity's container)
                //            calls RegionContext.UpdateEntity
                //                calls RegionContextBase.UpdateEntity
                //                    fires RegionContextBase.OnEntityUpdate
                //                        (World subscribes to the region entity container to get updates)
                //                        calls World.Region_OnEntityUpdate
                //                            fires World.OnEntityUpdate
                //                                (Viewer subscribes to entity updates to update display)
                //                                calls Viewer.World_OnEntityUpdate
                //                                    calls Renderer.RenderUpdate
                //                                        updates entity's pos and rot on screen
                //    calls World.Instance.UpdateAgent
                //        fires World.OnAgentUpdate
                //            calls Viewer.World_OnAgentUpdate
                //                (the camera follows the agent which just happens to be the avatar)
                //                calls mainCameraUpdate(with agent pos and rot)
                //                    fires CameraControl.OnCameraUpdate
                //                        (camera changes show up on the screen)
                //                        calls Renderer.UpdateCamera
                //                            calls into renderer to update view camera position
                //                        (camera changes are sent to simulator for culling optimizations)
                //                        calls Viewer.CameraControl_OnCameraUpdate
                //                            calls LLAgent.UpdateCamera
                //                                sends camera interest info (pos and rot) to simulator
                ContainingEntity.Update(UpdateCodes.Rotation);
            }
        }

        private float CalcMoveFudge() {
            // TODO: test if client is running or flying and return the correct fudge
            return m_moveFudge;
        }

        public void Dispose() {
            return;
        }
    }
}

