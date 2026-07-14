using UnityEngine;
using Miniscript;
using Sirenix.OdinInspector;

[ExecuteAlways]
public class MiniScriptTest : MonoBehaviour
{
    Interpreter m_Interpreter;

    void OnValidate()
    {
        if (m_Interpreter == null)
            m_Interpreter = new Interpreter();
    }

    [Button("Compile")]
    void RunMiniScript()
    {
        m_Interpreter?.Stop();

        m_Interpreter.standardOutput = (string s, bool lineBreak) => Debug.Log("[MiniScript]" + s);
        m_Interpreter.implicitOutput = (string s, bool lineBreak) => Debug.Log("[MiniScript]" + s);
        m_Interpreter.errorOutput = (string s, bool lineBreak) => Debug.LogError("[MiniScript]" + s);

        string helloWorldCode = "print \"Hello World!\"";
        m_Interpreter.Reset(helloWorldCode);
        m_Interpreter.Compile();

        Debug.Log("[LOG] MiniScript compiled successfully.");

        try
        {
            m_Interpreter.RunUntilDone();
        }
        catch (MiniscriptException e)
        {
            Debug.LogError(e);
        }
    }
}
