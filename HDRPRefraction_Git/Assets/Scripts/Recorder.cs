using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;

public class Recorder
{
    public string skyboxPath;
    public float skyMul;
    public Vector3 cameraPosition;
    public Vector3 cameraRotation;
    public string tableMat;
    public Vector3 tableScale;
    public int numberOfObjects;
    public List<string> categories;
    public List<string> prefabPaths;
    public List<Vector3> positions;
    public List<Vector3> rotations;
    public List<float> scales;
    public List<float> IORs;

    private string[] splitWords;
    private char[] splitChars;

    public Recorder()
    {
        skyboxPath = "";
        skyMul = 0.5f;
        cameraPosition = Vector3.zero;
        cameraRotation = Vector3.zero;
        tableMat = "";
        tableScale = Vector3.one;
        numberOfObjects = 0;
        categories = new List<string>();
        prefabPaths = new List<string>();
        positions = new List<Vector3>();
        rotations = new List<Vector3>();
        scales = new List<float>();
        IORs = new List<float>();

        splitChars = new char[2]{'\n', ' '};
    }

    public void WriteFile(string filePath)
    {
        WriteFileWithoutClear(filePath);
        Clear();
    }

    public void WriteFileWithoutClear(string filePath)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(skyboxPath + "\n");
        stringBuilder.Append(string.Format("{0} {1} {2}\n", cameraPosition.x, cameraPosition.y, cameraPosition.z));
        stringBuilder.Append(string.Format("{0} {1} {2}\n", cameraRotation.x, cameraRotation.y, cameraRotation.z));
        stringBuilder.Append(numberOfObjects.ToString() + "\n");
        for (int i = 0; i < numberOfObjects; ++i)
        {
            stringBuilder.Append(categories[i] + "\n");
            stringBuilder.Append(prefabPaths[i] + "\n");
            stringBuilder.Append(string.Format("{0} {1} {2}\n", positions[i].x, positions[i].y, positions[i].z));
            stringBuilder.Append(string.Format("{0} {1} {2}\n", rotations[i].x, rotations[i].y, rotations[i].z));
            stringBuilder.Append(string.Format("{0}\n", scales[i]));
            stringBuilder.Append(string.Format("{0}\n", IORs[i]));
        }
        using (StreamWriter sw = new StreamWriter(filePath))
        {
            sw.Write(stringBuilder.ToString());
        }
    }

    public void ParseFile(string filePath)
    {
        Clear();
        
        using (StreamReader sr = new StreamReader(filePath))
        {
            // Skybox path
            GetLine(sr);
            skyboxPath = splitWords[0];
            // Camera Position
            GetLine(sr);
            cameraPosition = ParseVector3();
            // Camera Rotation
            GetLine(sr);
            cameraRotation = ParseVector3();
            // Number of objects
            GetLine(sr);
            numberOfObjects = Convert.ToInt32(splitWords[0]);
            for (int i = 0; i < numberOfObjects; ++i)
            {
                // Category
                GetLine(sr);
                categories.Add(splitWords[0]);
                // Prefab path
                GetLine(sr);
                prefabPaths.Add(splitWords[0]);
                // Position
                GetLine(sr);
                positions.Add(ParseVector3());
                // Rotation
                GetLine(sr);
                rotations.Add(ParseVector3());
                // Scale
                GetLine(sr);
                scales.Add(Convert.ToSingle(splitWords[0]));
                // IOR
                GetLine(sr);
                IORs.Add(Convert.ToSingle(splitWords[0]));
            }
        }
    }

    public void Show()
    {
        Debug.Log(string.Format("Skyox path: {0}", skyboxPath));
        Debug.Log(string.Format("Camera position: {0} {1} {2}", cameraPosition.x, cameraPosition.y, cameraPosition.z));
        Debug.Log(string.Format("Camera rotation: {0} {1} {2}", cameraRotation.x, cameraRotation.y, cameraRotation.z));
        Debug.Log(string.Format("Number of objects: {0}", numberOfObjects));
        for (int i = 0; i < numberOfObjects; ++i)
        {
            Debug.Log(string.Format(
                "Object {0}: {1} Position: <{2} {3} {4}> Rotation: <{5} {6} {7}> Scale: {8} IOR: {9}", 
                new System.Object[10]{
                    i + 1, 
                    prefabPaths[i], 
                    positions[i].x, positions[i].y, positions[i].z, 
                    rotations[i].x, rotations[i].y, rotations[i].z, 
                    scales[i], 
                    IORs[i]
                    }
                ));
        }
    }

    public void Clear()
    {
        categories.Clear();
        prefabPaths.Clear();
        positions.Clear();
        rotations.Clear();
        scales.Clear();
        IORs.Clear();
    }

    private void GetLine(StreamReader sr)
    {
        splitWords = sr.ReadLine().Split(splitChars);
    }

    private Vector3 ParseVector3()
    {
        return new Vector3(
            Convert.ToSingle(splitWords[0]), 
            Convert.ToSingle(splitWords[1]), 
            Convert.ToSingle(splitWords[2])
            );
    }
}
