using UnityEngine;

namespace IDC
{
    public class Demo1Controller : MonoBehaviour
    {
        public GameObject cube;

        void Start()
        {
            IDCUtils.IDC.AddClass(this);
        }

        void Update()
        {

        }

        [IDCCmd]
        void AddCube(Vector3 pos, Color color)
        {
            var go = Instantiate(cube);
            go.transform.position = pos;

            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.material.color = color;
        }

        [IDCCmd]
        void SetCubeMaterial(GameObject go, Material m)
        {
            go.GetComponent<MeshRenderer>().material = m;
        }

        [IDCCmd]
        void RemoveCube(GameObject go)
        {
            Destroy(go);
        }

        [IDCCmd("Explode", "Creates an explosion at pos")]
        void ExplodeRbs(Vector3 pos, float explosionForce)
        {
#if UNITY_2020_2_OR_NEWER
            var cubes = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
#else
            var cubes = FindObjectsOfType<Rigidbody>();
#endif
            foreach (var c in cubes)
            {
                c.AddExplosionForce(explosionForce, pos, 100);
            }
        }
    }
}