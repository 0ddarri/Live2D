using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

struct LiveVertex
{
    public Vector3 position;
    public Vector2 uv;

    public BoneWeight weight;
}

struct LiveBone
{
    public Matrix4x4 bindPosition;
    public Transform bonePosition;
}

public class Debugger : MonoBehaviour
{
    public GameObject live2DObject;
    public GameObject vertexVisualizer;
    public GameObject bonePrefab;
    public GameObject boneParent;
    private GameObject quad;

    public Dropdown boneDropdown;
    int previousBoneIndex = 0;
    public Dropdown vertexDropdown;
    int currentVertexIndex = 0;
    public Slider weightSlider;
    public InputField boneIndexInput;
    public Mesh staticMesh;

    List<LiveVertex> liveVertices;
    List<LiveBone> liveBones;

    public GameObject[] obj;
    bool drawVertexAndLine = true;

    public Dropdown selectBoneForParentDropdown1;
    public Dropdown selectBoneForParentDropdown2;

    void DrawVertexAndLine(Vector3[] vertices)
    {
        Vector3[] worldVertices = new Vector3[vertices.Length];

        for (int i = 0; i < vertices.Length; ++i)
        {
            worldVertices[i] = live2DObject.transform.TransformPoint(vertices[i]);
            GameObject visualizer = Instantiate(vertexVisualizer, worldVertices[i], Quaternion.identity);
            visualizer.transform.SetParent(transform);
            if (i == currentVertexIndex)
                visualizer.GetComponent<SpriteRenderer>().color = Color.green;
            else
                visualizer.GetComponent<SpriteRenderer>().color = Color.red;
        }

        Mesh mesh = live2DObject.GetComponent<MeshFilter>().mesh;
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length / 3; ++i)
        {
            GameObject lineRendererObject = new GameObject();
            lineRendererObject.transform.SetParent(transform);
            lineRendererObject.name = "LineRender";
            LineRenderer renderer = lineRendererObject.AddComponent<LineRenderer>();
            renderer.startColor = Color.black;
            renderer.endColor = Color.black;
            renderer.startWidth = 0.005f;
            renderer.endWidth = 0.005f;
            renderer.gameObject.layer = 5;
            renderer.material = new Material(Shader.Find("Diffuse"));
            renderer.positionCount = 4;

            for (int j = 0; j < 4; ++j)
            {
                if (j != 3)
                    renderer.SetPosition(j, worldVertices[triangles[(i * 3) + j]]);
                else
                    renderer.SetPosition(j, worldVertices[triangles[(i * 3)]]);
            }
        }
    }

    void InitVertexWeight(ref LiveVertex vertex)
    {
        vertex.weight.boneIndex0 = 0;
        vertex.weight.boneIndex1 = -1;
        vertex.weight.boneIndex2 = -1;
        vertex.weight.boneIndex3 = -1;

        vertex.weight.weight0 = 1;
        vertex.weight.weight1 = 0;
        vertex.weight.weight2 = 0;
        vertex.weight.weight3 = 0;
    }

    void SetWeightAccumulateIn1(ref LiveVertex vertex)
    {
        float weightAcc = vertex.weight.weight0 + vertex.weight.weight1 + vertex.weight.weight2 + vertex.weight.weight3;
        if (weightAcc > 1.0f)
        {
            BoneWeight w = vertex.weight;
            float length = Mathf.Sqrt((w.weight0 * w.weight0) + (w.weight1 * w.weight1) + (w.weight2 * w.weight2) + (w.weight3 * w.weight3));
            vertex.weight.weight0 = w.weight0 / length;
            vertex.weight.weight1 = w.weight1 / length;
            vertex.weight.weight2 = w.weight2 / length;
            vertex.weight.weight3 = w.weight3 / length;
        }
    }

    public void AddBone()
    {
        LiveBone bone = new LiveBone();
        GameObject boneGameObject = Instantiate(bonePrefab, Vector3.zero, Quaternion.identity);
        boneGameObject.transform.parent = boneParent.transform;

        boneGameObject.GetComponent<SpriteRenderer>().color = Color.red;

        bone.bonePosition = boneGameObject.transform;
        // ������Ʈ �ʿ�
        bone.bindPosition = Matrix4x4.identity;
        liveBones.Add(bone);

        UpdateDropdowns();
    }

    public void AttachVertexToBone()
    {
        int boneIndex = boneDropdown.value;
        int vertexIndex = vertexDropdown.value;
        float weight = weightSlider.value;
        int input = int.Parse(boneIndexInput.transform.GetChild(2).GetComponent<Text>().text);

        LiveVertex vertex = liveVertices[vertexIndex];

        if (input == 0)
        {
            vertex.weight.boneIndex0 = boneIndex;
            vertex.weight.weight0 = weight;
        }
        else if (input == 1)
        {
            vertex.weight.boneIndex1 = boneIndex;
            vertex.weight.weight1 = weight;
        }
        else if (input == 2)
        {
            vertex.weight.boneIndex2 = boneIndex;
            vertex.weight.weight2 = weight;
        }
        else if (input == 3)
        {
            vertex.weight.boneIndex3 = boneIndex;
            vertex.weight.weight3 = weight;
        }
        SetWeightAccumulateIn1(ref vertex);
        liveVertices[vertexIndex] = vertex;
    }

    void UpdateDropdowns()
    {
        vertexDropdown.ClearOptions();
        for (int i = 0; i < liveVertices.Count; ++i)
        {
            Dropdown.OptionData data = new Dropdown.OptionData(string.Format("Vertex {0}", i));
            vertexDropdown.options.Add(data);
        }
        vertexDropdown.transform.GetChild(0).GetComponent<Text>().text = vertexDropdown.options[0].text;

        boneDropdown.ClearOptions();
        selectBoneForParentDropdown1.ClearOptions();
        selectBoneForParentDropdown2.ClearOptions();
        for (int i = 0; i < liveBones.Count; ++i)
        {
            Dropdown.OptionData data = new Dropdown.OptionData(string.Format("Bone {0}", i));
            boneDropdown.options.Add(data);
            selectBoneForParentDropdown1.options.Add(data);
            selectBoneForParentDropdown2.options.Add(data);
        }

        if (boneDropdown.options.Count != 0)
        {
            boneDropdown.transform.GetChild(0).GetComponent<Text>().text = boneDropdown.options[0].text;
            selectBoneForParentDropdown1.transform.GetChild(0).GetComponent<Text>().text = boneDropdown.options[0].text;
            selectBoneForParentDropdown2.transform.GetChild(0).GetComponent<Text>().text = boneDropdown.options[0].text;
        }
    }

    public void Play()
    {
        for (int i = 0; i < liveBones.Count; ++i)
        {
            LiveBone bone = liveBones[i];
            bone.bindPosition = bone.bonePosition.localToWorldMatrix;
            liveBones[i] = bone;
        }
    }

    public void ChangeVertexColor()
    {
        currentVertexIndex = vertexDropdown.value;
    }

    public void ChangeBoneColor()
    {
        liveBones[previousBoneIndex].bonePosition.gameObject.GetComponent<SpriteRenderer>().color = Color.red;

        liveBones[boneDropdown.value].bonePosition.gameObject.GetComponent<SpriteRenderer>().color = Color.green;
        previousBoneIndex = boneDropdown.value;
    }

    public void AddVertex(Vector3 worldPosition)
    {
        // ���忡�� ���÷� ��ȯ�� ��, live2DObject mesh�� �߰�
        List<Vector3> triVtxPos;
        List<Vector2> triUV;
        if (IsPointLiesInMesh(worldPosition, out triVtxPos, out triUV))
        {
            obj[0].transform.position = triVtxPos[0];
            obj[1].transform.position = triVtxPos[1];
            obj[2].transform.position = triVtxPos[2];

            Debug.Log("INSIDE!!!");

            Mesh mesh = live2DObject.GetComponent<MeshFilter>().mesh;
            int verticesLength = mesh.vertexCount + 1;

            Vector3[] vertices = new Vector3[verticesLength];
            mesh.vertices.CopyTo(vertices, 0);

            Vector4 worldPos;
            worldPos.x = worldPosition.x;
            worldPos.y = worldPosition.y;
            worldPos.z = 0;
            worldPos.w = 1;
            Vector3 P = live2DObject.transform.worldToLocalMatrix * worldPos;
            vertices[mesh.vertices.Length] = P;

            Vector2[] uv = new Vector2[verticesLength];
            mesh.uv.CopyTo(uv, 0);

            // Cramer's Rule
            Vector3 v0 = triVtxPos[0] - triVtxPos[2];
            Vector3 v1 = triVtxPos[1] - triVtxPos[2];
            Vector3 v2 = P - triVtxPos[2];
            float denominator = (Vector3.Dot(v0, v0) * Vector3.Dot(v1, v1)) - (Vector3.Dot(v1, v0) * Vector3.Dot(v0, v1));
            float s = ((Vector3.Dot(v2, v0) * Vector3.Dot(v1, v1)) - (Vector3.Dot(v1, v0) * Vector3.Dot(v2, v1))) / denominator;
            float t = ((Vector3.Dot(v0, v0) * Vector3.Dot(v2, v1)) - (Vector3.Dot(v2, v0) * Vector3.Dot(v0, v1))) / denominator;

            uv[mesh.uv.Length] = s * triUV[0] + t * triUV[1] + (1 - s - t) * triUV[2];

            mesh.vertices = vertices;
            mesh.uv = uv;

            LiveVertex vtx = new LiveVertex();
            vtx.position = P;
            vtx.uv = new Vector2(0.5f, 0.5f);
            InitVertexWeight(ref vtx);
            liveVertices.Add(vtx);

            UpdateDropdowns();

            List<DelaunayTriangulation.Vertex> triangulationData = new List<DelaunayTriangulation.Vertex>();

            List<int> indecies = new List<int>();

            for (int i = 0; i < liveVertices.Count; ++i)
            {
                Vector3 position = liveVertices[i].position;
                triangulationData.Add(new DelaunayTriangulation.Vertex(new Vector2(position.x, position.y), i));
            }

            DelaunayTriangulation.Triangulation triangulation = new DelaunayTriangulation.Triangulation(triangulationData);

            foreach (DelaunayTriangulation.Triangle triangle in triangulation.triangles)
            {
                indecies.Add(triangle.vertex0.index);
                indecies.Add(triangle.vertex1.index);
                indecies.Add(triangle.vertex2.index);
            }

            mesh.SetIndices(indecies.ToArray(), MeshTopology.Triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            staticMesh = mesh;
        }
        else
        {
            Debug.Log("NOTINSIDE");
        }
    }

    bool IsPointLiesInMesh(Vector3 worldPosition, out List<Vector3> triVtxPos, out List<Vector2> triUV)
    {
        triVtxPos = new List<Vector3>();
        triUV = new List<Vector2>();

        // live2DObject�� Triangle�� �� ���鼭 �˻�
        Mesh mesh = live2DObject.GetComponent<MeshFilter>().mesh;
        int[] triangles = mesh.triangles;

        Vector4 worldPos;
        worldPos.x = worldPosition.x;
        worldPos.y = worldPosition.y;
        worldPos.z = 0;
        worldPos.w = 1;
        Vector3 P = live2DObject.transform.worldToLocalMatrix * worldPos;

        for (int i = 0; i < triangles.Length / 3; ++i)
        {
            Vector3 A = mesh.vertices[triangles[(i * 3)]];
            Vector3 B = mesh.vertices[triangles[(i * 3) + 1]];
            Vector3 C = mesh.vertices[triangles[(i * 3) + 2]];

            // �ﰢ���� ���� ABC == PAB + PBC + PAC��� P�� ABC �ȿ� �ִ�.
            Vector3 AB = B - A;
            Vector3 AC = C - A;
            float ABCarea = 0.5f * (Vector3.Cross(AB,AC)).magnitude;
            float roundABCarea = Mathf.Round(ABCarea * 10.0f) * 0.1f;
            Vector3 PA = A - P;
            Vector3 PB = B - P;
            Vector3 PC = C - P;
            float PABarea = 0.5f * (Vector3.Cross(PA, PB)).magnitude;
            float PBCarea = 0.5f * (Vector3.Cross(PB, PC)).magnitude;
            float PACarea = 0.5f * (Vector3.Cross(PA, PC)).magnitude;
            
            // ���� ������ �����ϱ� ������, �ݿø�
            float result = Mathf.Round((PABarea + PBCarea + PACarea) * 10.0f) * 0.1f;
            if (roundABCarea >= result)
            {
                triVtxPos.Add(A);
                triVtxPos.Add(B);
                triVtxPos.Add(C);

                triUV.Add(mesh.uv[triangles[(i * 3)]]);
                triUV.Add(mesh.uv[triangles[(i * 3) + 1]]);
                triUV.Add(mesh.uv[triangles[(i * 3) + 2]]);
                return true;
            }
        }
        return false;
    }

    public void SetBoneParent()
    {
        liveBones[selectBoneForParentDropdown2.value].bonePosition.parent = liveBones[selectBoneForParentDropdown1.value].bonePosition;
    


        //LiveBone previousBone = liveBones[previousBoneIndex];
        //previousBone.color = Color.red;
        //liveBones[previousVertexIndex] = previousBone;
       
        boneParent.transform.GetChild(previousBoneIndex).GetComponent<SpriteRenderer>().color = Color.red;

        //LiveBone firstBone = liveBones[boneDropdown.value];
        //firstBone.color = Color.green;
        //liveBones[boneDropdown.value] = firstBone;

        boneParent.transform.GetChild(boneDropdown.value).GetComponent<SpriteRenderer>().color = Color.green;
        previousBoneIndex = boneDropdown.value;
    }


    // Start is called before the first frame update
    void Start()
    {
        liveVertices = new List<LiveVertex>();
        liveBones = new List<LiveBone>();

        // ���� ����
        quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.SetActive(false);

        Mesh mesh = Instantiate<Mesh>(quad.GetComponent<MeshFilter>().mesh);
        live2DObject.GetComponent<MeshFilter>().mesh = mesh;
        staticMesh = mesh;

        // Live ������ ����
        Vector3[] vertices = mesh.vertices;

        LiveVertex v1 = new LiveVertex();
        v1.position = vertices[0];
        v1.uv = mesh.uv[0];
        liveVertices.Add(v1);

        LiveVertex v2 = new LiveVertex();
        v2.position = vertices[1];
        v2.uv = mesh.uv[1];
        liveVertices.Add(v2);

        LiveVertex v3 = new LiveVertex();
        v3.position = vertices[2];
        v3.uv = mesh.uv[2];
        liveVertices.Add(v3);

        LiveVertex v4 = new LiveVertex();
        v4.position = vertices[3];
        v4.uv = mesh.uv[3];
        liveVertices.Add(v4);

        for (int i = 0; i < 4; ++i)
        {
            LiveVertex vtx = liveVertices[i];
            InitVertexWeight(ref vtx);
            liveVertices[i] = vtx;
        }

        LiveBone bone = new LiveBone();
        bone.bonePosition = boneParent.transform.GetChild(0).transform;
        bone.bindPosition = bone.bonePosition.localToWorldMatrix;
        liveBones.Add(bone);

        UpdateDropdowns();
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        Mesh mesh = Instantiate<Mesh>(staticMesh);
        Vector3[] vertices = mesh.vertices;

        // ���� ���� ������
        for (int i = 0; i < vertices.Length; ++i)
        {
            // ��ġ�� ���� ��Ű�� ���� ����
            Vector3 totalPosition = Vector4.zero;
            BoneWeight w = liveVertices[i].weight;
            for (int j = 0; j < 4; ++j)
            {
                int boneIndex = 0;
                float boneWeight = 0;

                if (j == 0)
                {
                    if (w.boneIndex0 == -1)
                        break;
                    boneIndex = w.boneIndex0;
                    boneWeight = w.weight0;
                }
                else if (j == 1)
                {
                    if (w.boneIndex1 == -1)
                        break;
                    boneIndex = w.boneIndex1;
                    boneWeight = w.weight1;
                }
                else if (j == 2)
                {
                    if (w.boneIndex2 == -1)
                        break;
                    boneIndex = w.boneIndex2;
                    boneWeight = w.weight2;
                }
                else if (j == 3)
                {
                    if (w.boneIndex3 == -1)
                        break;
                    boneIndex = w.boneIndex3;
                    boneWeight = w.weight3;
                }

                if (boneIndex >= liveBones.Count)
                    continue;

                Matrix4x4 boneMatrix = liveBones[boneIndex].bonePosition.localToWorldMatrix; // ����
                Matrix4x4 boneBindMatrix = liveBones[boneIndex].bindPosition; // ����

                Vector4 currentVertex = vertices[i];
                currentVertex.w = 1;
                // ���� ���� ��ġ
                Vector4 localPosition = boneBindMatrix.inverse * currentVertex;

                Vector3 skinnedWorldPosition = boneMatrix * localPosition;

                totalPosition += skinnedWorldPosition * boneWeight;
            }

            vertices[i] = totalPosition;
        }
        mesh.vertices = vertices;
        live2DObject.GetComponent<MeshFilter>().mesh = mesh;

        if (drawVertexAndLine)
            DrawVertexAndLine(mesh.vertices);

        if (Input.GetKeyDown(KeyCode.Q))
            drawVertexAndLine = !drawVertexAndLine;
    }
}
