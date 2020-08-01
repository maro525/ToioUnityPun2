using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace toio.Simulator
{
    public class CubeSimulator : MonoBehaviour
    {
        #pragma warning disable 0414
        #pragma warning disable 0649

        // --- Physical Constants ---
        // from https://toio.github.io/toio-spec/docs/
        public static readonly float TireWidthM = 0.0266f;
        public static readonly float TireWidthDot= 0.0266f * Mat.DotPerM;
        public static readonly float WidthM= 0.0318f;
        // ratio of Speed(Dot/s) and order ( 2.04f in real test )
        // theorically, 4.3 rpm/u * pi * 0.0125m / (60s/m) * DotPerM
        public static readonly float VDotOverU =  4.3f*Mathf.PI*0.0125f/60 * Mat.DotPerM; // about 2.06
        public float MOTOR_TAU = 0.04f; // parameter of one-order model for motor, τ
        public float delay = 0.15f; // latency of communication

        // --- Parameters ---
        public int maxMotor { get{
            if (version == Version.v2_0_0) return 100;
            else if (version == Version.v2_1_0) return 115;
            else return 100;
        }}
        public int deadzone { get{
            if (version == Version.v2_0_0) return 10;
            else if (version == Version.v2_1_0) return 8;
            else return 10;
        }}

        // --- Properties ---
        public int x { get; private set; }
        public int y { get; private set; }
        public float deg { get; private set; }
        public bool offMat { get; private set; }  // off Mat (in unity space)
        public Mat mat { get; private set; }
        public bool ready { get; private set; }

        // --- unity objects ---
        private Rigidbody rb;
        private AudioSource audioSource;
        private GameObject LED;
        private BoxCollider col;

        // --- 物理状態 ---
        private float speedL = 0;  // (M)
        private float speedR = 0;

        // --- オーダー処理用 ---
        // motor
        private float motorLeft = 0;
        private float motorRight = 0;
        private float motorDuration = 0;
        private float motorTimeElipsed = 0;
        private Queue<int> motorLeftQ = new Queue<int>();
        private Queue<int> motorRightQ = new Queue<int>();
        private Queue<int> motorDurationQ = new Queue<int>();
        private Queue<float> motorTimeQ = new Queue<float>();

        // light
        private Cube.LightOperation[] lights = null;
        private int lightRepeat;
        private float lightTimeElipsed = 0;
        private int lightRepeatedCnt = 0;
        private bool lightLasting = false;
        private Queue<Cube.LightOperation[]> lightsQ = new Queue<Cube.LightOperation[]>();
        private Queue<int> lightRepeatQ = new Queue<int>();
        private Queue<bool> lightLastingQ = new Queue<bool>();
        private Queue<float> lightTimeQ = new Queue<float>();

        // sound
        private List<Cube.SoundOperation[]> presetSounds = new List<Cube.SoundOperation[]>();
        private Cube.SoundOperation[] sounds = null;
        private int playingSound=-1;
        private int soundRepeat;
        private float soundTimeElipsed = 0;
        private int soundRepeatedCnt = 0;
        private Queue<Cube.SoundOperation[]> soundsQ = new Queue<Cube.SoundOperation[]>();
        private Queue<int> soundRepeatQ = new Queue<int>();
        private Queue<float> soundTimeQ = new Queue<float>();

        public enum Version
        {
            v2_0_0,
            v2_1_0
        }
        public Version version;

        private void Start()
        {
            this.ready = false;

            #if !UNITY_EDITOR
                this.gameObject.SetActive(false);
            #else
                this.offMat = false;
                this.rb = GetComponent<Rigidbody>();
                this.rb.maxAngularVelocity = 21f;
                this.audioSource = GetComponent<AudioSource>();
                this._InitPresetSounds();
                this.LED = transform.Find("LED").gameObject;
                this.LED.GetComponent<Renderer>().material.color = Color.black;
                this.col = GetComponent<BoxCollider>();
            #endif
        }

        private void Update()
        {
        }

        private void FixedUpdate()
        {
            // ==== 時間経過　Time elapse ====
            float dt = Time.deltaTime;
            float currentTime = Time.time;

            motorTimeElipsed += dt;     // motor
            lightTimeElipsed += dt;     // light
            soundTimeElipsed += dt;     // sound

            // ==== 通信遅延を模擬　Simulate comunication delay ====
            // --- motor order ---
            while (motorTimeQ.Count > 0 && currentTime > motorTimeQ.Peek() + delay ){
                motorTimeElipsed = 0;
                motorDuration = motorDurationQ.Dequeue()/1000f;
                motorLeft = motorLeftQ.Dequeue();
                motorRight = motorRightQ.Dequeue();
                motorTimeQ.Dequeue();
            }
            // --- light ---
            while (lightTimeQ.Count > 0 && currentTime > lightTimeQ.Peek() + delay ){
                lightTimeElipsed = 0;
                lights = lightsQ.Dequeue();
                lightRepeat = lightRepeatQ.Dequeue();
                lightTimeQ.Dequeue();
                lightLasting = lightLastingQ.Dequeue();
            }
            // --- sound ---
            while (soundTimeQ.Count > 0 && currentTime > soundTimeQ.Peek() + delay){
                soundTimeElipsed = 0;
                sounds = soundsQ.Dequeue();
                soundRepeat = soundRepeatQ.Dequeue();
                soundTimeQ.Dequeue();
            }

            // ==== オーダーを実行　Excute Orders ====
            // --- Simulate Physics ---
            if (motorTimeElipsed > motorDuration && motorDuration > 0){
                motorLeft = 0; motorRight = 0;
            }
            UpdatePhysic(dt);

            // --- Light ---
            if (lights == null)  _StopLight();
            else if (lightLasting){
                if (lightTimeElipsed==0)
                    // Turn on Light
                    _SetLight(lights[0].red, lights[0].green, lights[0].blue);
            }
            else
            {
                // Calc. period
                float period = 0;
                for (int i=0; i<lights.Length; ++i){
                    period += lights[i].durationMs/1000f;
                }
                if (period==0){
                    lightRepeatedCnt = 0;
                    lights = null;
                    _StopLight();
                }
                // Next repeat?
                if (lightTimeElipsed >= period){
                    lightRepeatedCnt += (int)(lightTimeElipsed/period);
                    lightTimeElipsed %= period;
                }
                // Repeat over
                if (lightRepeatedCnt >= lightRepeat && lightRepeat > 0){
                    lightRepeatedCnt = 0;
                    lights = null;
                    _StopLight();
                }
                else if (lights != null)
                {
                    // Index of current operation
                    float sum = 0; int index=0;
                    for (int i=0; i<lights.Length; ++i){
                        sum += lights[i].durationMs/1000f;
                        if (lightTimeElipsed < sum){
                            index = i;
                            break;
                        }
                    }
                    // Turn on Light
                    _SetLight(lights[index].red, lights[index].green, lights[index].blue);
                }
            }

            // --- Sound ---
            if (sounds == null) _StopSound();
            else
            {
                // Calc. period
                float period = 0;
                for (int i=0; i<sounds.Length; ++i){
                    period += sounds[i].durationMs/1000f;
                }
                if (period == 0){
                    soundRepeatedCnt = 0;
                    sounds = null;
                    _StopSound();
                }
                // Next repeat?
                if (soundTimeElipsed >= period)
                {
                    soundRepeatedCnt += (int)(soundTimeElipsed/period);
                    soundTimeElipsed %= period;
                }
                // Repeat over
                if (soundRepeatedCnt >= soundRepeat && soundRepeat > 0)
                {
                    soundRepeatedCnt = 0;
                    sounds = null;
                    _StopSound();
                }
                else if (sounds != null)
                {
                    // Index of current operation
                    float sum = 0; int index=0;
                    for (int i=0; i<sounds.Length; ++i){
                        sum += sounds[i].durationMs/1000f;
                        if (soundTimeElipsed < sum){
                            index = i;
                            break;
                        }
                    }
                    // Play
                    int sound = sounds[index].note_number;
                    if (sound != playingSound){
                        playingSound = sound;
                        if (sound >= 128) _StopSound();
                        else _PlaySound(sound, sounds[index].volume);
                    }
                }
            }
        }

        // 物理計算　オーダーを変更する前に呼んでください。
        // Physics (Call this before Changing Orders (motorLeft, motorRight, motorTimeLeft)! )
        private void UpdatePhysic(float dt)
        {
            // 座標、角度情報更新
            // retrieve coord
            RaycastHit hit;
            Ray ray = new Ray(transform.position+transform.up*0.001f, -transform.up);

            if (Physics.Raycast(ray, out hit)) {
                float dist = (hit.point - ray.origin).magnitude;
                if (hit.transform.gameObject.tag == "Mat" && dist < 0.005f){
                    this.offMat = false;
                    this.mat = hit.transform.gameObject.GetComponent<Mat>();
                    var coord = this.mat.UnityCoord2MatCoord(transform.position);
                    this.x = coord.x; this.y = coord.y;
                    this.deg = this.mat.UnityDeg2MatDeg(transform.eulerAngles.y);
                }
                else this.offMat = true;
            }
            else this.offMat = true;

            // 速度を更新
            // update speed
            float targetSpeedL = motorLeft * VDotOverU / Mat.DotPerM;
            float targetSpeedR = motorRight * VDotOverU / Mat.DotPerM;
            if (Mathf.Abs(motorLeft) < deadzone) targetSpeedL = 0;
            if (Mathf.Abs(motorRight) < deadzone) targetSpeedR = 0;

            speedL += (targetSpeedL - speedL) / Mathf.Max(MOTOR_TAU,dt) * dt;
            speedR += (targetSpeedR - speedR) / Mathf.Max(MOTOR_TAU,dt) * dt;

            // 速度変化によって力を与え、位置と角度を更新
            // speed -> postion
            if (!this.offMat) {
                // transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
                this.rb.angularVelocity = transform.up * (float)((speedL - speedR) / TireWidthM);
                var vel = transform.forward * (speedL + speedR) / 2;
                var dv = vel - this.rb.velocity;
                // this.rb.velocity = new Vector3(vel.x, rb.velocity.y, vel.z);
                this.rb.AddForce(dv, ForceMode.VelocityChange);
            }
            else{
                // this.rb.angularVelocity = new Vector3(0, 0, 0);
                // this.rb.velocity = new Vector3(0, rb.velocity.y, 0);
            }
            this.ready = true;
        }

        // ====== インターフェイス ======

        /// <summary>
        /// モーター：時間指定付きモーター制御
        /// </summary>
        public void Move(int left, int right, int durationMS)
        {
            if (version >= Version.v2_0_0)
            {
                motorDurationQ.Enqueue(Mathf.Clamp(durationMS, 0, 2550));
                motorLeftQ.Enqueue( Mathf.Clamp(left, -maxMotor, maxMotor));
                motorRightQ.Enqueue( Mathf.Clamp(right, -maxMotor, maxMotor));
                motorTimeQ.Enqueue(Time.time);
            }
        }

        /// <summary>
        /// ランプ：消灯
        /// </summary>
        public void StopLight()
        {
            if (version >= Version.v2_0_0)
            {
                Cube.LightOperation[] ops = new Cube.LightOperation[1];
                ops[0] = new Cube.LightOperation(100, 0, 0, 0);
                lightsQ.Enqueue(ops);
                lightRepeatQ.Enqueue(1);
                lightTimeQ.Enqueue(Time.time);
                lightLastingQ.Enqueue(false);
            }
        }
        /// <summary>
        /// ランプ：点灯
        /// </summary>
        public void SetLight(int r, int g, int b, int durationMS)
        {
            if (version >= Version.v2_0_0)
            {
                durationMS = Mathf.Clamp(durationMS / 10, 0, 255)*10;
                Cube.LightOperation[] ops = new Cube.LightOperation[1];
                ops[0] = new Cube.LightOperation((short)durationMS, (byte)r, (byte)g, (byte)b);
                lightsQ.Enqueue(ops);
                lightRepeatQ.Enqueue(1);
                lightTimeQ.Enqueue(Time.time);
                lightLastingQ.Enqueue(durationMS==0);
            }
        }
        /// <summary>
        /// ランプ：連続的な点灯・消灯
        /// </summary>
        public void SetLights(int repeatCount, Cube.LightOperation[] operations)
        {
            if (operations.Length == 0) return;
            repeatCount = Mathf.Clamp(repeatCount, 0, 255);
            if (version >= Version.v2_0_0)
            {
                operations = operations.Take(29).ToArray();
                lightsQ.Enqueue(operations);
                lightRepeatQ.Enqueue((byte)repeatCount);
                lightTimeQ.Enqueue(Time.time);
                lightLastingQ.Enqueue(false);
            }
        }

        /// <summary>
        /// サウンド：MIDI note number の再生
        /// </summary>
        public void PlaySound(int repeatCount, Cube.SoundOperation[] operations)
        {
            if (operations.Length == 0) return;
            repeatCount = Mathf.Clamp(repeatCount, 0, 255);
            if (version >= Version.v2_0_0)
            {
                operations = operations.Take(59).ToArray();
                soundsQ.Enqueue(operations);
                soundRepeatQ.Enqueue(repeatCount);
                soundTimeQ.Enqueue(Time.time);
            }
        }

        /// <summary>
        /// サウンド：効果音の再生 （未実装）
        /// </summary>
        public void PlayPresetSound(int soundId, int volume)
        {
            soundId = BitConverter.GetBytes(soundId)[0];
            if (version >= Version.v2_0_0)
            {
                if (this.presetSounds.Count == 0) return;
                if (soundId >= this.presetSounds.Count) soundId = 0;
                PlaySound(1, this.presetSounds[soundId]);
            }
        }
        /// <summary>
        /// サウンド：再生の停止
        /// </summary>
        public void StopSound()
        {
            if (version >= Version.v2_0_0)
            {
                Cube.SoundOperation[] ops = new Cube.SoundOperation[1];
                ops[0] = new Cube.SoundOperation(100, 0, 128);
                soundsQ.Enqueue(ops);
                soundRepeatQ.Enqueue(1);
                soundTimeQ.Enqueue(Time.time);
            }
        }


        // ====== 内部関数 ======

        private void _SetLight(int r, int g, int b){
            r = Mathf.Clamp(r, 0, 255);
            g = Mathf.Clamp(g, 0, 255);
            b = Mathf.Clamp(b, 0, 255);
            LED.GetComponent<Renderer>().material.color = new Color(r/255f, g/255f, b/255f);
        }

        private void _StopLight(){
            LED.GetComponent<Renderer>().material.color = Color.black;
        }

        private void _PlaySound(int soundId, int volume){
            int octave = (int)(soundId/12);
            int idx = (int)(soundId%12);
            var aCubeOnSlot = Resources.Load("Octave/" + (octave*12+9)) as AudioClip;
            audioSource.volume = (float)volume/256;
            audioSource.pitch = (float)Math.Pow(2, ((float)idx-9)/12);
            audioSource.clip = aCubeOnSlot;
            audioSource.Play();
        }
        private void _StopSound(){
            audioSource.clip = null;
            audioSource.Stop();
        }

        // Sound Preset を設定
        private void _InitPresetSounds(){
            Cube.SoundOperation[] sounds = new Cube.SoundOperation[3];
            sounds[0] = new Cube.SoundOperation(200, 255, 48);
            sounds[1] = new Cube.SoundOperation(200, 255, 50);
            sounds[2] = new Cube.SoundOperation(200, 255, 52);
            this.presetSounds.Add(sounds);
        }


    }
}