using UnityEngine;
using toio.Simulator;

namespace toio
{
    public class CubeUnity : Cube
    {
        GameObject gameObject;
        CubeSimulator simulator;

        // ※全コールバック未実装
        // シミュレータでワーニングログが出てしまうためダミーを用意
        CallbackProvider _buttonCallback = new CallbackProvider();
        CallbackProvider _slopeCallback = new CallbackProvider();
        CallbackProvider _collisionCallback = new CallbackProvider();
        CallbackProvider _idCallback = new CallbackProvider();
        CallbackProvider _standardIdCallback = new CallbackProvider();

        public CubeUnity(GameObject gameObject)
        {
            this.gameObject = gameObject;
            id = gameObject.name;
            simulator = gameObject.GetComponent<CubeSimulator>();
        }

        public override string id { get; protected set; }
        public override int battery { get { return 100; } protected set { } }
        public override string version { get {
                if (simulator.version == CubeSimulator.Version.v2_0_0) return "2.0.0";
                else if (simulator.version == CubeSimulator.Version.v2_1_0) return "2.1.0";
                return "2.0.0";
        } }
        public override int x { get { return simulator.x; } protected set { } }
        public override int y { get { return simulator.y; } protected set { } }
        public override Vector2 pos { get { return new Vector2(x, y); } }
        public override int angle { get { return (int)(simulator.deg + 0.49); } protected set { } }
        public override Vector2 sensorPos { get { return new Vector2(x, y); } }
        public override int sensorAngle { get { return (int)(simulator.deg + 0.49); } protected set { } }
        public override uint standardId { get { return 0; } protected set { } }
        public override bool isSloped { get; protected set; }
        public override bool isPressed { get; protected set; }
        public override bool isCollisionDetected { get; protected set; }
        public override bool isGrounded { get { return true; } protected set { } }
        public override int maxSpd { get { return simulator.maxMotor; } }

        public bool offStage { get { return simulator.offMat; } }

        // 未実装
        public override CallbackProvider buttonCallback { get { return this._buttonCallback; } }
        public override CallbackProvider slopeCallback { get { return this._slopeCallback; } }
        public override CallbackProvider collisionCallback { get { return this._collisionCallback; } }
        public override CallbackProvider idCallback { get { return this._idCallback; } }
        public override CallbackProvider standardIdCallback { get { return this._standardIdCallback; } }

        ////////////  API  ///////////////

        public override void Move(int left, int right, int durationMs, ORDER_TYPE order = ORDER_TYPE.Weak)
        {
#if RELEASE
            CubeOrderBalancer.Instance.AddOrder(this, () => simulator.Move(left, right, durationMs), order);
#else
            CubeOrderBalancer.Instance.DEBUG_AddOrderParams(this, () => simulator.Move(left, right, durationMs), order, "move", left, right);
#endif
        }

        // Sound
        public override void PlaySound(int repeatCount, SoundOperation[] operations, ORDER_TYPE order = ORDER_TYPE.Weak)
        {
            repeatCount = Mathf.Clamp(repeatCount, 0, 255);

#if RELEASE
            CubeOrderBalancer.Instance.AddOrder(this, () => simulator.PlaySound(repeatCount, operations), order);
#else
            CubeOrderBalancer.Instance.DEBUG_AddOrderParams(this, () => simulator.PlaySound(repeatCount, operations), order, "playSound", repeatCount);
#endif
        }

        public override void PlaySound(byte[] buff, ORDER_TYPE order = ORDER_TYPE.Weak)
        {
            var repeat = buff[1];
            var length = buff[2];

            int start = 3;
            var data = new SoundOperation[length];
            for (int i = 0; i < length; i++)
            {
                data[i] = new SoundOperation();
                data[i].durationMs = (short)(buff[start + i * 3] * 10);
                data[i].note_number = buff[start + i * 3 + 1];
                data[i].volume = buff[start + i * 3 + 2];
            }


#if RELEASE
            CubeOrderBalancer.Instance.AddOrder(this, () => simulator.PlaySound(repeat, data), order);
#else
            CubeOrderBalancer.Instance.DEBUG_AddOrderParams(this, () => simulator.PlaySound(repeat, data), order, "playSound", repeat);
#endif
        }

        public override void PlayPresetSound(int soundId, int volume = 255, ORDER_TYPE order = ORDER_TYPE.Weak)
        {
#if RELEASE
            CubeOrderBalancer.Instance.AddOrder(this, () => simulator.PlayPresetSound(soundId, volume), order);
#else
            CubeOrderBalancer.Instance.DEBUG_AddOrderParams(this, () => simulator.PlayPresetSound(soundId, volume), order, "playPresetSound", soundId);
#endif
        }

        public override void StopSound(ORDER_TYPE order = ORDER_TYPE.Weak)
        {
#if RELEASE
            CubeOrderBalancer.Instance.AddOrder(this, () => simulator.StopSound(), order);
#else
            CubeOrderBalancer.Instance.DEBUG_AddOrderParams(this, () => simulator.StopSound(), order, "stopSound");
#endif
        }

        // Light
        public override void TurnLedOff(ORDER_TYPE order = ORDER_TYPE.Weak)
        {
#if RELEASE
            CubeOrderBalancer.Instance.AddOrder(this, () => simulator.StopLight(), order);
#else
            CubeOrderBalancer.Instance.DEBUG_AddOrderParams(this, () => simulator.StopLight(), order, "turnLedOff");
#endif
        }

        public override void TurnLedOn(int red, int green, int blue, int durationMs, ORDER_TYPE order = ORDER_TYPE.Weak)
        {
#if RELEASE
            CubeOrderBalancer.Instance.AddOrder(this, () => simulator.SetLight(red, green, blue, duration), order);
#else
            CubeOrderBalancer.Instance.DEBUG_AddOrderParams(this, () => simulator.SetLight(red, green, blue, durationMs), order, "turnLedOn", red, green, blue, durationMs);
#endif
        }

        public override void TurnOnLightWithScenario(int repeatCount, Cube.LightOperation[] operations, ORDER_TYPE order = ORDER_TYPE.Weak)
        {
#if RELEASE
            CubeOrderBalancer.Instance.AddOrder(this, () => simulator.SetLights(repeatCount, operations), order);
#else
            CubeOrderBalancer.Instance.DEBUG_AddOrderParams(this, () => simulator.SetLights(repeatCount, operations), order, "turnOnLightWithScenario", repeatCount, operations);
#endif
        }
        public override void ConfigSlopeThreshold(int angle, ORDER_TYPE order = ORDER_TYPE.Strong) { }
        public override void ConfigCollisionThreshold(int level, ORDER_TYPE order = ORDER_TYPE.Strong) { }

        //  no use
        public override string addr { get { return id; } }
        public override bool isConnected { get { return simulator.ready; } }
    }
}