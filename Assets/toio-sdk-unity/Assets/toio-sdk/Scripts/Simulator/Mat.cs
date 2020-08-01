using UnityEngine;

namespace toio.Simulator
{

    /// <summary>
    /// マットオブジェクトに一対一に付けてください。
    /// </summary>
    public class Mat : MonoBehaviour
    {
        public enum MatType
        {
            MainFront,  // toio 本体マットの正面
            MainBack,  // toio 本体マットの背面
            //Gezon,
            Custom  // 座標範囲をカスタマイズ
        }

        public static readonly float DotPerM = 411f/0.560f; // 411/0.560 dot/m
        public MatType matType;
        public int xMin, xMax, yMin, yMax;
        public float xCenter { get{ return (xMin+xMax)/2f; } }
        public float yCenter { get{ return (yMin+yMax)/2f; } }

        void Start()
        {
            this.ApplyMatType();
        }

        void Update()
        {

        }

        /// <summary>
        /// マットのタイプ、座標範囲の変更を反映
        /// </summary>
        internal void ApplyMatType()
        {
            switch (matType){
                case MatType.MainFront:
                    xMin = 45; xMax = 455; yMin = 45; yMax = 455;
                    GetComponent<Renderer>().material = (Material)Resources.Load<Material>("mat_front");;
                    break;
                case MatType.MainBack:
                    xMin = 545; xMax = 955; yMin = 45; yMax = 455;
                    GetComponent<Renderer>().material = (Material)Resources.Load<Material>("mat_back");
                    break;
                case MatType.Custom:
                    GetComponent<Renderer>().material = (Material)Resources.Load<Material>("mat_null");
                    break;
            }
            this.transform.localScale = new Vector3((xMax-xMin+1)/DotPerM, (yMax-yMin+1)/DotPerM, 1);
        }


        // ==== 角度変換関数 ====

        /// <summary>
        /// Unity上の角度を本マット上の角度に変換
        /// </summary>
        public int UnityDeg2MatDeg(double degU)
        {
            return (int)(degU-this.transform.eulerAngles.y-90+0.49999f)%360;
        }
        /// <summary>
        /// Unity上の角度をマットmat上の角度に変換
        /// </summary>
        public static int UnityDeg2MatDeg(double degU, Mat mat)
        {
            if (mat == null) return (int)(degU-90)%360;
            else return mat.UnityDeg2MatDeg(degU);
        }

        /// <summary>
        /// 本マット上の角度をUnity上の角度に変換
        /// </summary>
        public float MatDeg2UnityDeg(double degM)
        {
            return (int)(degM+this.transform.eulerAngles.y+90+0.49999f)%360;
        }
        /// <summary>
        /// マットmat上の角度をUnity上の角度に変換
        /// </summary>
        public static float MatDeg2UnityDeg(double degM, Mat mat)
        {
            if (mat == null) return (float)(degM+90)%360;
            else return mat.MatDeg2UnityDeg(degM);
        }


        // ==== 座標変換関数 ====

        /// <summary>
        /// Unity の3D空間座標から、本マットにおけるマット座標に変換。
        /// </summary>
        public Vector2Int UnityCoord2MatCoord(Vector3 unityCoord)
        {
            var matPos = this.transform.position;
            var drad = - this.transform.eulerAngles.y * Mathf.Deg2Rad;
            var _cos = Mathf.Cos(drad);
            var _sin = Mathf.Sin(drad);

            // 座標系移動：本マットに一致させ
            var dx = unityCoord[0] - matPos[0];
            var dy = -unityCoord[2] + matPos[2];

            // 座標系回転：本マットに一致させ
            Vector2 coord = new Vector2(dx*_cos-dy*_sin, dx*_sin+dy*_cos);

            // マット単位に変換
            return new Vector2Int(
                (int)(coord.x*DotPerM + this.xCenter + 0.4999f),
                (int)(coord.y*DotPerM + this.yCenter + 0.4999f)
            );
        }

        /// <summary>
        /// マット mat におけるマット座標から、Unity の3D空間に変換。mat が null の場合、mat.prefab の初期位置に基づく。
        /// </summary>
        public static Vector3 MatCoord2UnityCoord(double x, double y, Mat mat)
        {
            if (mat==null)
                return new Vector3((float)(x-250)/DotPerM, 0, -(float)(y-250)/DotPerM);
            else
            {
                return mat.MatCoord2UnityCoord(x, y);
            }
        }
        public static Vector3 MatCoord2UnityCoord(Vector2Int matCoord, Mat mat)
        {
            if (mat==null)
                return new Vector3((matCoord.x-250)/DotPerM, 0, -(matCoord.y-250)/DotPerM);
            else
            {
                return mat.MatCoord2UnityCoord(matCoord.x, matCoord.y);
            }
        }

        /// <summary>
        /// 本マットにおけるマット座標から、Unity の3D空間に変換。
        /// </summary>
        public Vector3 MatCoord2UnityCoord(double x, double y)
        {
            var matPos = this.transform.position;
            var drad = this.transform.eulerAngles.y * Mathf.Deg2Rad;
            var _cos = Mathf.Cos(drad);
            var _sin = Mathf.Sin(drad);

            // メーター単位に変換
            var dx = ((float)x - xCenter)/DotPerM;
            var dy = ((float)y - yCenter)/DotPerM;

            // 座標系回転：Unityに一致させ
            Vector2 coord = new Vector2(dx*_cos-dy*_sin, dx*_sin+dy*_cos);

            // 座標系移動：Unityに一致させ
            coord.x += matPos.x;
            coord.y += -matPos.z;

            return new Vector3(coord.x, matPos.y, -coord.y);
        }

    }

}
