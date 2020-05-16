using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[RequireComponent (typeof (MeshFilter))]
[RequireComponent (typeof (MeshRenderer))]
public class LDrawPart : MonoBehaviour {
    private const int ColourCount = 50; // Must cover the range of all colours available

    // Public properties
    public string filename = string.Empty;

    public int defaultColour = 16;

    public GameObject self = null;

    // Mesh fields
    private List<Vector3> meshVertices = new List<Vector3> ();
    private List<int>[] meshTriangles = new List<int>[ColourCount];
    private List<int>[] meshLines = new List<int>[ColourCount];

    // Start is called before the first frame update
    void Start () {
        this.ParseFile (this.filename);
    }

    private void ParseFile (string filename) {
        char[] delimeters = new char[] { ' ', '\t' };

        // Determine the full path
        filename = this.ResolvePartPath ("", filename);

        StreamReader reader = new StreamReader (filename, System.Text.Encoding.UTF8);
        string line;
        while ((line = reader.ReadLine ()) != null) {
            // Sanitise the line
            // Debug.Log (line);
            line = line.Trim (delimeters);
            if (string.IsNullOrWhiteSpace (line)) { continue; }

            // Determine the type and tokenize
            char type = line.Length == 0 ? char.MinValue : line[0];

            switch (type) {
                case '0': // Comment or meta command
                    {
                        string[] tokens = line.Split (delimeters, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length == 1) { continue; } // Not interested if there is no textual component
                        this.ProcessComment (tokens[1]);
                        continue;
                    }

                case '1': // Sub-file reference
                    {
                        string[] tokens = line.Split (delimeters, 15, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length != 15) { break; } // Not enough tokens
                        this.ProcessSubFile (tokens);
                        continue;
                    }

                case '2': // Line
                    {
                        string[] tokens = line.Split (delimeters, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length != 8) { break; } // Not the right amount of tokens
                        this.ProcessLinePrimitive (type, tokens);
                        continue;
                    }

                case '3': // Triangle
                    {
                        string[] tokens = line.Split (delimeters, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length != 11) { break; } // Not the right amount of tokens
                        this.ProcessTrianglePrimitive (tokens);
                        continue;
                    }

                case '4': // Quadrilateral
                    {
                        string[] tokens = line.Split (delimeters, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length != 14) { break; } // Not the right amount of tokens
                        this.ProcessQuadPrimitive (tokens);
                        continue;
                    }

                case '5': // Optional Line
                    {
                        string[] tokens = line.Split (delimeters, StringSplitOptions.RemoveEmptyEntries);
                        if (tokens.Length != 14) { break; } // Not the right amount of tokens
                        this.ProcessLinePrimitive (type, tokens);
                        continue;
                    }
            }

            // We couldn't parse it so it's invalid
            if (type == char.MinValue) {
                Debug.LogErrorFormat ("Invalid line: {0}", line);
            } else {
                Debug.LogErrorFormat ("Invalid type '{0}' line: {1}", type, line);
            }
        }

        // Do we need to create a mesh
        if (meshVertices.Count > 0) {
            this.processMesh ();
        }
    }

    #region Mesh processing
    private void processMesh () {
        // Do we need to create a mesh?
        if (meshVertices.Count == 0) { return; }

        // Determine how many submeshes and materials we need
        int materialCount = 0;
        for (int colourIndex = 0; colourIndex < ColourCount; colourIndex++) {
            if (
                this.meshTriangles[colourIndex] != null ||
                this.meshLines[colourIndex] != null ||
                colourIndex == 16 || colourIndex == 24 // Always create entries for 16 and 24
            ) {
                materialCount++;
            }
        }

        // Get/Create a mesh filter and renderer
        MeshFilter filter = this.GetComponent<MeshFilter> ();
        if (filter == null) {
            filter = this.gameObject.AddComponent<MeshFilter> ();
        }
        MeshRenderer renderer = this.GetComponent<MeshRenderer> ();
        if (renderer == null) {
            renderer = this.gameObject.AddComponent<MeshRenderer> ();
        }

        // And the mesh itself
        Mesh mesh = new Mesh ();
        filter.mesh = mesh;
        mesh.subMeshCount = materialCount;
        Material[] materials = new Material[materialCount];
        int meshIndex = 0;

        // Set all the vertices
        mesh.vertices = this.meshVertices.ToArray ();

        // Add the inherited colours (16 and 24) first
        this.AddColourMesh (meshIndex++, mesh, ref materials, 16);
        this.AddColourMesh (meshIndex++, mesh, ref materials, 24);

        // And each other colour's triangles and lines
        int maxColourIndex = Math.Max (this.meshTriangles.Length, this.meshLines.Length);
        for (int colourIndex = 0; colourIndex < maxColourIndex; colourIndex++) {
            if (colourIndex == 16 || colourIndex == 24) { continue; }
            this.AddColourMesh (meshIndex++, mesh, ref materials, colourIndex);
        }

        // Set the materials on the renderer
        renderer.materials = materials;
    }

    private void AddColourMesh (int index, Mesh mesh, ref Material[] materials, int colourIndex) {
        bool addMaterial = false;

        // And each colour's triangles and lines
        if (colourIndex < this.meshTriangles.Length) {
            List<int> triangles = this.meshTriangles[colourIndex];
            if (triangles != null && triangles.Count > 0) {
                mesh.SetTriangles (triangles.ToArray (), index);
                addMaterial = true;
            }
        }

        if (colourIndex < this.meshLines.Length) {
            List<int> lines = this.meshLines[colourIndex];
            if (lines != null && lines.Count > 0) {
                mesh.SetIndices (lines.ToArray (), MeshTopology.Lines, index);
                addMaterial = true;
            }
        }

        if (addMaterial) {
            // FIXME Look these up by name?
            string[] materialIds = new string[ColourCount];
            materialIds[0] = "509779034e0b8294b83e26e528eba4d2";
            materialIds[1] = "f5d58533b3c270f4a8196d6edb036ceb";
            materialIds[2] = "7f0e7f6d5e1eb3943b341eeca971fa6c";
            materialIds[3] = "42c59cb6b1d2bca4a9b9df87553d636d";
            materialIds[4] = "d7f9a43fd026900478232a532028a206";
            materialIds[7] = "196af0c0dd986974582a57b11d8f993c";
            materialIds[14] = "54ecd5e05fce25e41969f9940f35ffde";
            materialIds[36] = "4b19ef2d2cb1e29449226048ef694308";
            materialIds[39] = "3b6eac4755aa0ce4e8501cecab0d6688";
            materialIds[46] = "21d3790cd6f0e5044a4d05b8d6874f25";

            // The inherited colours get re-mapped, but stay separate
            if (colourIndex == 16) { colourIndex = this.defaultColour; }

            // Get the material
            string materialId = materialIds[colourIndex];
            string materialPath = UnityEditor.AssetDatabase.GUIDToAssetPath (materialId);
            Material material = (Material) UnityEditor.AssetDatabase.LoadAssetAtPath (materialPath, typeof (Material));

            // Store it in the renderer's array
            materials[index] = material;
        }
    }
    #endregion Mesh processing

    #region Line type helpers
    private void ProcessComment (string content) {
        // Create an empty object for the string
        GameObject newObject = new GameObject ("// " + content);
        newObject.transform.SetParent (this.transform);
    }

    private void ProcessSubFile (string[] tokens) {
        // Split into components
        string color = tokens[1];
        string[] positionTokens = new string[3];
        Array.Copy (tokens, 2, positionTokens, 0, 3);
        string[] matrixTokens = new string[9];
        Array.Copy (tokens, 5, matrixTokens, 0, 9);
        string subFilename = tokens[14];

        string path = subFilename; // this.ResolvePartPath (this.filename, subFilename);
        if (string.IsNullOrEmpty (path)) {
            Debug.LogErrorFormat ("Unable to locate sub part {0}", subFilename);
            GameObject newObject2 = new GameObject (string.Join (" ", tokens));
            newObject2.transform.SetParent (this.transform);
            return;
        }

        // Convert the matrix into Unity objects
        Vector3 position;
        Quaternion rotation;
        Vector3 scale;
        this.CreateTransformData (positionTokens, matrixTokens, out position, out rotation, out scale);

        // Create the sub object
        GameObject newObject = Instantiate (this.self, position, rotation, this.transform);
        newObject.name = string.Join (" ", tokens);
        newObject.transform.localScale = scale;
        newObject.transform.localPosition = position;
        newObject.transform.localRotation = rotation;

        // Add the part data
        newObject.GetComponent<LDrawPart> ().self = this.self;
        newObject.GetComponent<LDrawPart> ().filename = path;

        // Set the part colour
        int partColour = int.Parse (color);
        if (partColour == 16) { partColour = this.defaultColour; }
        newObject.GetComponent<LDrawPart> ().defaultColour = partColour;
    }

    private void ProcessLinePrimitive (char type, string[] tokens) {
        // Split into components
        string color = tokens[1];
        string[][] pointTokens = new string[][] { new string[3], new string[3] };
        Array.Copy (tokens, 2, pointTokens[0], 0, 3);
        Array.Copy (tokens, 5, pointTokens[1], 0, 3);

        // Update the mesh
        int startIndex = this.meshVertices.Count;
        this.meshVertices.Add (this.CreateVertex (pointTokens[0]));
        this.meshVertices.Add (this.CreateVertex (pointTokens[1]));

        List<int> lines = this.meshLines[int.Parse (color)];
        if (lines == null) {
            lines = this.meshLines[int.Parse (color)] = new List<int> ();
        }

        lines.Add (startIndex);
        lines.Add (startIndex + 1);

        // Create the sub object
        GameObject newObject = new GameObject (string.Join (" ", tokens));
        newObject.transform.SetParent (this.transform);
    }

    private void ProcessTrianglePrimitive (string[] tokens) {
        // Split into components
        string color = tokens[1];
        string[][] pointTokens = new string[][] { new string[3], new string[3], new string[3] };
        Array.Copy (tokens, 2, pointTokens[0], 0, 3);
        Array.Copy (tokens, 5, pointTokens[1], 0, 3);
        Array.Copy (tokens, 8, pointTokens[2], 0, 3);

        // Update the mesh
        int startIndex = this.meshVertices.Count;
        this.meshVertices.Add (this.CreateVertex (pointTokens[0]));
        this.meshVertices.Add (this.CreateVertex (pointTokens[1]));
        this.meshVertices.Add (this.CreateVertex (pointTokens[2]));

        List<int> triangles = this.meshTriangles[int.Parse (color)];
        if (triangles == null) {
            triangles = this.meshTriangles[int.Parse (color)] = new List<int> ();
        }

        // CW
        // 0 1
        //   2
        triangles.Add (startIndex);
        triangles.Add (startIndex + 1);
        triangles.Add (startIndex + 2);

        // CCW
        // 0  
        // 1 2
        triangles.Add (startIndex);
        triangles.Add (startIndex + 2);
        triangles.Add (startIndex + 1);

        // Create the sub object
        GameObject newObject = new GameObject (string.Join (" ", tokens));
        newObject.transform.SetParent (this.transform);
    }

    private void ProcessQuadPrimitive (string[] tokens) {
        // Split into components
        string color = tokens[1];
        string[][] pointTokens = new string[][] { new string[3], new string[3], new string[3], new string[3] };
        Array.Copy (tokens, 2, pointTokens[0], 0, 3);
        Array.Copy (tokens, 5, pointTokens[1], 0, 3);
        Array.Copy (tokens, 8, pointTokens[2], 0, 3);
        Array.Copy (tokens, 11, pointTokens[3], 0, 3);

        // Update the mesh
        int startIndex = this.meshVertices.Count;
        this.meshVertices.Add (this.CreateVertex (pointTokens[0]));
        this.meshVertices.Add (this.CreateVertex (pointTokens[1]));
        this.meshVertices.Add (this.CreateVertex (pointTokens[2]));
        this.meshVertices.Add (this.CreateVertex (pointTokens[3]));

        List<int> triangles = this.meshTriangles[int.Parse (color)];
        if (triangles == null) {
            triangles = this.meshTriangles[int.Parse (color)] = new List<int> ();
        }

        // CW
        // 0 1
        // 3 2
        triangles.Add (startIndex);
        triangles.Add (startIndex + 1);
        triangles.Add (startIndex + 2);

        triangles.Add (startIndex);
        triangles.Add (startIndex + 2);
        triangles.Add (startIndex + 3);

        // CCW
        // 0 3
        // 1 2
        triangles.Add (startIndex);
        triangles.Add (startIndex + 3);
        triangles.Add (startIndex + 2);

        triangles.Add (startIndex);
        triangles.Add (startIndex + 2);
        triangles.Add (startIndex + 1);

        // Create the sub object
        GameObject newObject = new GameObject (string.Join (" ", tokens));
        newObject.transform.SetParent (this.transform);
    }
    #endregion Line type helpers

    #region Matrix conversion
    private Vector3 CreateVertex (string[] pointTokens) {
        if (pointTokens.Length != 3) { throw new ArgumentException ("Invalid point token array", "pointTokens"); }

        float x = float.Parse (pointTokens[0]);
        float y = float.Parse (pointTokens[1]);
        float z = float.Parse (pointTokens[2]);

        // LDraw uses a right-handed co-ordinate system where -Y is "up".
        return new Vector3 (x, -y, z);
    }

    private void CreateTransformData (string[] pointTokens, string[] matrixTokens, out Vector3 position, out Quaternion rotation, out Vector3 scale) {
        if (pointTokens.Length != 3) { throw new ArgumentException ("Invalid point token array", "pointTokens"); }
        if (matrixTokens.Length != 9) { throw new ArgumentException ("Invalid matrix token array", "matrixTokens"); }

        // Convert the matrix into Unity objects
        Matrix4x4 rotationMatrix = new Matrix4x4 (
            new Vector4 (float.Parse (matrixTokens[0]), float.Parse (matrixTokens[3]), float.Parse (matrixTokens[6]), 0),
            new Vector4 (float.Parse (matrixTokens[1]), float.Parse (matrixTokens[4]), float.Parse (matrixTokens[7]), 0),
            new Vector4 (float.Parse (matrixTokens[2]), float.Parse (matrixTokens[5]), float.Parse (matrixTokens[8]), 0),
            new Vector4 (float.Parse (pointTokens[0]), float.Parse (pointTokens[1]), float.Parse (pointTokens[2]), 1)
        );

        // LDraw uses a right-handed co-ordinate system where -Y is "up".
        position = new Vector3 (rotationMatrix.m03, -rotationMatrix.m13, rotationMatrix.m23);
        rotation = rotationMatrix.rotation;
        scale = rotationMatrix.lossyScale;

        // FIXME Hack for negative scaling that Unity gets wrong. Look for simple scaling and create the vector and reset the rotation
        if (
            rotationMatrix.m01 == 0 && rotationMatrix.m02 == 0 &&
            rotationMatrix.m10 == 0 && rotationMatrix.m12 == 0 &&
            rotationMatrix.m20 == 0 && rotationMatrix.m21 == 0
        ) {
            Vector3 correctScale = new Vector3 (rotationMatrix.m00, rotationMatrix.m11, rotationMatrix.m22);
            if (scale != correctScale) {
                Debug.LogFormat ("Incorrectly interpreted matrix: Should be {0} but got {1}.\n{2}", correctScale, scale, rotationMatrix.ToString ());
                rotation = Quaternion.identity;
                scale = correctScale;
            }
        }
    }
    #endregion Matrix conversion

    #region LDraw library functions
    private string ResolvePartPath (string currentPart, string subPart) {
        string ldrawPath = @"C:\Users\dee\Documents\Unity\VLDraw\ldraw\";
        string path;

        // Check relative paths
        if (!string.IsNullOrEmpty (currentPart)) {
            path = Path.Combine (Path.GetDirectoryName (currentPart), subPart);
            if (File.Exists (path)) { return path; }
        }

        // Is it a part?
        path = Path.Combine (ldrawPath, @"parts\", subPart);
        if (File.Exists (path)) { return path; }

        // Primitive?
        path = Path.Combine (ldrawPath, @"p\", subPart);
        if (File.Exists (path)) { return path; }

        // Try on its own
        path = Path.Combine (ldrawPath, subPart);
        if (File.Exists (path)) { return path; }

        return string.Empty;
    }
    #endregion LDraw library functions
}