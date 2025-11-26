
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Nomlas.ColliderBlocker
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class CollierBlocker : UdonSharpBehaviour
    {
        public bool IsUsingCollider() => usingCollider;

        [Header("コライダーを検出")][SerializeField] private bool enableColliderDetection;
        [Header("コライダーダッシュを禁止")][SerializeField] private bool disableColliderDash;
        [Header("許容速度")][SerializeField] private float allowedSpeed = 0.5f;
        [Header("コライダー検出時の動作")]
        [Header("ジャンプを無効化")][SerializeField] private bool disableJump;
        [Header("プレイヤーを固定")][SerializeField] private bool immobilizePlayer;
        [Header("検出時に表示するGameObject")][SerializeField] private GameObject[] showOnUsingCheat;

        private const int layer = 1 << 10; // Layer 10
        private const float radius = 5.0f;
        private const int threshold = 2;
        private VRCPlayerApi localPlayer;
        private bool usingCollider;
        private bool previousUsingCollider;
        private float jumpImpulse;
        void Start()
        {
            localPlayer = Networking.LocalPlayer;
        }

        void Update()
        {
            if (disableColliderDash)
            {
                var runSpeed = localPlayer.GetRunSpeed(); // VRCWorldで設定された走る速度
                var maxSpeed = runSpeed + allowedSpeed; // 若干の速度超過を許容
                var playerVel = localPlayer.GetVelocity(); // 現在のプレイヤーの速度を取得
                Vector2 playerVelXZ = new Vector2(playerVel.x, playerVel.z); // XZ平面上で確認
                var usingDash = playerVelXZ.sqrMagnitude > (maxSpeed * maxSpeed); 
                if (usingDash)
                {
                    var setVel = localPlayer.GetRotation() * Vector3.forward * runSpeed;
                    setVel.y = playerVel.y;
                    localPlayer.SetVelocity(setVel); // 速度を上書き
                }
            }
            if (enableColliderDetection)
            {
                var pos = localPlayer.GetPosition();
                var hitCount = Physics.OverlapSphere(pos, radius, layer).Length;
                usingCollider = hitCount > threshold; //コライダー数でチェック（多くの場合、Udon VMによってnullが返されるので注意）
                if (usingCollider != previousUsingCollider)
                {
                    OnUsingColliderChanged();
                    previousUsingCollider = usingCollider;
                }
            }
        }
        private void OnUsingColliderChanged()
        {
            if (usingCollider)
            {
                if (disableJump)
                {
                    jumpImpulse = localPlayer.GetJumpImpulse();
                    localPlayer.SetJumpImpulse(0); //ジャンプの強さをゼロにする
                }
                if (immobilizePlayer) localPlayer.Immobilize(true); //プレイヤーを固定
                SetActiveObjects(true);
            }
            else
            {
                if (disableJump)
                {
                    localPlayer.SetJumpImpulse(jumpImpulse); //元のジャンプの強さに戻す
                }
                if (immobilizePlayer) localPlayer.Immobilize(false); //プレイヤーの固定を解除
                SetActiveObjects(false);
            }
        }

        private void SetActiveObjects(bool isActive)
        {
            foreach (var obj in showOnUsingCheat)
            {
                obj.SetActive(isActive);
            }
        }
    }
}