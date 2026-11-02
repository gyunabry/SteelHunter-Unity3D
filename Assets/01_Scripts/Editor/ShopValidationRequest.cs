using System;
using System.IO;
using UnityEditor;

// Opt-in request file supports validation when no Editor RPC bridge is available.
[InitializeOnLoad]
public static class ShopValidationRequest
{
    static ShopValidationRequest() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        const string request = "Temp/ShopValidation.request";
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
        File.Delete(request);
        try
        {
            ShopSetup.Apply();
            ShopValidation.Run();
            File.WriteAllText("Temp/ShopValidation.result", "PASS: setup and shop transaction validation completed.");
        }
        catch (Exception exception)
        {
            File.WriteAllText("Temp/ShopValidation.result", exception.ToString());
            UnityEngine.Debug.LogException(exception);
        }
    }
}
