using UnityEngine;


namespace toio.Simulator
{

    public class Stage : MonoBehaviour
    {
        private Transform targetPole;
        private GameObject mainLightObj;
        private GameObject sideLightObj;
        private GameObject backLightObj;
        public Mat mat { get; private set; }
        public Transform focusTarget = null;

        void Start()
        {
            // #if !UNITY_EDITOR
            //     this.gameObject.SetActive(false);
            //     return;
            // #endif

            var transforms = gameObject.GetComponentsInChildren<Transform>();
            foreach (var tf in transforms){
                var go = tf.gameObject;
                if (go.GetComponent<Light>()!=null){
                    if (go.name == "Spot Light Main")
                        this.mainLightObj = go;
                    else if (go.name == "Spot Light Side")
                        this.sideLightObj = go;
                    else if (go.name == "Spot Light Back")
                        this.backLightObj = go;
                }
                else if (go.name == "TargetPole" && targetPole==null){
                    this.targetPole = tf;
                }
                else if (go.name == "Mat"){
                    this.mat = go.GetComponent<Mat>();

                }
            }

        }

        void Update()
        {
            // 左クリックでターゲットポールを設置
            // Left mouse to Move Target Pole
            if (Input.GetMouseButton(0)){
                var camera = GameObject.FindObjectOfType<Camera>();
                RaycastHit hit;
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out hit) && targetPole != null) {
                    targetPole.position = new Vector3(hit.point.x, targetPole.position.y, hit.point.z);
                }
            }

            // 右クリックでキューブを選択
            // Right Click to Select Cube
            if (Input.GetMouseButtonDown(1)){
                var camera = GameObject.FindObjectOfType<Camera>();
                RaycastHit hit;
                Ray ray = camera.ScreenPointToRay(Input.mousePosition);
                Stage stage = GameObject.FindObjectOfType<Stage>();

                if (Physics.Raycast(ray, out hit)) {
                    if (hit.transform.gameObject.tag == "Cube")
                        stage.SetFocus(hit.transform);
                    else stage.SetNoFocus();
                }
                else stage.SetNoFocus();

            }

            // ターゲットを追従
            // Keep focusing on focusTarget
            if (focusTarget!=null){
                var tar = new Vector3(0, 0.01f, 0) + focusTarget.position;
                mainLightObj.GetComponent<Light>().transform.LookAt(tar);
                sideLightObj.GetComponent<Light>().transform.LookAt(tar);
            }
        }

        /// <summary>
        /// Make Lights focus on input transfrom, and the transform can be retrieved by property "focusTarget".
        /// </summary>
        public void SetFocus(Transform transform){
            mainLightObj.GetComponent<Light>().spotAngle = 6;
            sideLightObj.GetComponent<Light>().spotAngle = 6;
            focusTarget = transform;
        }

        /// <summary>
        /// Cancel focus.
        /// </summary>
        public void SetNoFocus(){
            mainLightObj.GetComponent<Light>().spotAngle = 110;
            sideLightObj.GetComponent<Light>().spotAngle = 110;
            focusTarget = null;
        }

        /// <summary>
        /// Get name of currently focused game object.
        /// </summary>
        public string focusName { get{
            if (focusTarget == null) return null;
            else return focusTarget.gameObject.name;
        }}

        /// <summary>
        /// Get coord on mat of targetPole.
        /// </summary>
        public Vector2Int targetPoleCoord { get{
            if (targetPole != null)
                return this.mat.UnityCoord2MatCoord(targetPole.position);
            return new Vector2Int(250,250);
        }}
    }

}