using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using UnityEngine;

public class OSCQuery : MonoBehaviour
{
    [Header("Network settings")]
    public int oscQueryPort = 9010;
    public int oscPort = 9011;

    HttpListener listener;
    Thread serverThread;

    HttpListenerRequest currentRequest;
    string responseString;

    void Awake()
    {
        if (!HttpListener.IsSupported)
        {
            Debug.LogError("Http server not supported !");
            return;
        }

        listener = new HttpListener();
        listener.Prefixes.Add("http://*:"+oscQueryPort+"/");

        serverThread = new Thread(RunThread);

        generateResponse();

        Debug.Log("web server started");
    }

    private void OnEnable()
    {
        Debug.Log("Enable, start listener and thread");
        if (listener != null) listener.Start();
        if (serverThread != null) serverThread.Start();
    }

    private void OnDisable()
    {
        Debug.Log("Disable, stop listener and thread");
        if (serverThread != null) serverThread.Abort();
        if(listener != null) listener.Stop();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentRequest != null) responseString = generateResponse();
    }


    void RunThread()
    {
        Debug.Log("Run thread");
        currentRequest = null;
        responseString = "";

        while(true)
        {
            HttpListenerContext context = listener.GetContext();
            currentRequest = context.Request;
            
            while(responseString == "")
            {
                //wait
            }

            // Obtain a response object.

            HttpListenerResponse response = context.Response;
            response.AddHeader("Content-Type", "application/json");
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            System.IO.Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();

            currentRequest = null;
            responseString = "";

        }
    }

    string generateResponse()
    {
        JSONObject o = new JSONObject("root");
        o.SetField("ACCESS", 0);
        GameObject[] rootObjects =  UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        JSONObject co = new JSONObject();
        foreach (GameObject go in rootObjects) co.SetField(sanitizeName(go.name), getObjectData(go));

        o.SetField("CONTENTS", co);

        return o.ToString(true);
    }

    JSONObject getObjectData(GameObject go)
    {
        JSONObject o = new JSONObject();
        o.SetField("ACCESS", 0);

        JSONObject co = new JSONObject();
        for (int i = 0; i < go.transform.childCount; i++)
        {
            GameObject cgo = go.transform.GetChild(i).gameObject;
            Debug.Log("Child of " + go.name + " > " + cgo.name);
            co.SetField(sanitizeName(cgo.name), getObjectData(cgo));
        }

        Component[] comps = go.GetComponents<Component>();

        foreach(Component comp in comps)
        {
            int dotIndex = comp.GetType().ToString().LastIndexOf(".");
            string compType = comp.GetType().ToString().Substring(Mathf.Max(dotIndex+1, 0));

            JSONObject cco = new JSONObject();
            cco.SetField("ACCESS", 0);

            JSONObject ccco = new JSONObject();

            FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (FieldInfo info in fields)
            {
                RangeAttribute rangeAttribute = info.GetCustomAttribute<RangeAttribute>();
                JSONObject io = getPropObject(info.FieldType, info.GetValue(comp), rangeAttribute);

                if (io != null) ccco.SetField(sanitizeName(info.Name), io);
                else Debug.Log(info.Name + " skipped, type : " + info.GetType());
            }
           
            PropertyInfo[] props = comp.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo info in props)
            {
                string propType = info.PropertyType.ToString();
                if (propType == "UnityEngine.Component") continue; //fix deprecation error
                if (propType == "UnityEngine.GameObject") continue; //fix deprecation error
                if (propType == "UnityEngine.Matrix4x4") continue; //fix deprecation error
                if (propType == "UnityEngine.Transform") continue; //fix deprecation error
                if (info.Name == "name" || info.Name == "tag") continue;

                RangeAttribute rangeAttribute = info.GetCustomAttribute<RangeAttribute>();
                ccco.SetField(sanitizeName(info.Name), getPropObject(info.PropertyType, info.GetValue(comp), rangeAttribute));
            }

            cco.SetField("CONTENTS", ccco);
            co.SetField(sanitizeName(compType), cco);
        }

        o.SetField("CONTENTS", co);

        return o;
    }

    JSONObject getPropObject(System.Type type, object value, RangeAttribute range)
    {
        JSONObject po = new JSONObject();
        po.SetField("ACCESS", 3);

        JSONObject vo = new JSONObject();
        JSONObject ro = new JSONObject();
        JSONObject ro0 = new JSONObject();
        ro.Add(ro0);
        if (range != null)
        {
            ro0.Add(range.min);
            ro0.Add(range.max);
        }

        string typeString = type.ToString();
        string poType = "";



        switch (typeString)
        {
            case "System.String":
            case "System.Char":
                vo.Add(value.ToString());
                poType = "s";
                break;

            case "System.Boolean":
                vo.Add((bool)value);
                poType = "b";
                break;

            case "System.Int32":
            case "System.Int64":
            case "System.UInt32":
            case "System.Int16":
            case "System.UInt16":
            case "System.Byte":
            case "System.SByte":
                {
                    //add range
                    vo.Add((int)value);
                    poType = "i";
                }
            break;

            case "System.Double":
            case "System.Single":
                {
                    //add range
                    vo.Add((float)value);
                    poType = "f";
                }
                break;

            case "UnityEngine.Vector2":
                {
                    Vector2 v = (Vector2)value;
                    vo.Add(v.x);
                    vo.Add(v.y);
                    poType = "ff";
                }
                break;

            case "UnityEngine.Vector3":
                {
                    Vector3 v = (Vector3)value;
                    vo.Add(v.x);
                    vo.Add(v.y);
                    vo.Add(v.z);
                    poType = "fff";
                }
                break;

            case "UnityEngine.Quaternion":
                {
                    Vector3 v = ((Quaternion)value).eulerAngles;
                    vo.Add(v.x);
                    vo.Add(v.y);
                    vo.Add(v.z);
                    poType = "fff";
                }
                break;

            case "UnityEngine.Color":
                {
                    Color c = (Color)value;
                    vo.Add(ColorUtility.ToHtmlStringRGBA(c)); 
                    poType = "r";
                }
                break;


            default:
                if (type.IsEnum)
                {
                    JSONObject enumO = new JSONObject();

                    FieldInfo[] fields = type.GetFields();

                    foreach (var field in fields)
                    {
                        if (field.Name.Equals("value__")) continue;
                        enumO.Add(field.Name);
                    }
                    ro0.SetField("VALS", enumO);
                    vo.Add(value.ToString());
                    poType = "s";

                }
                else
                {
                   // Debug.LogWarning("Field type not supported " + typeString);
                    return null;
                }
                break;
                
        }

        po.SetField("VALUE", vo);
        po.SetField("TYPE", poType);
        po.SetField("RANGE", ro);
        return po;
    }

    string sanitizeName(string niceName)
    {
        return niceName.Replace(" ", "-");
    }
}

internal class MetadataTypeAttribute
{
}