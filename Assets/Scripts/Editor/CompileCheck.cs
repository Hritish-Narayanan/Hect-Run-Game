using UnityEditor;
using UnityEngine;
using UnityEditor.Compilation;

namespace SubwaySurfers.EditorTools
{
    /// <summary>
    /// Compile-check helper. Triggered from the editor via -executeMethod or the
    /// Tools menu; forces a domain reload and reports any compile errors.
    /// </summary>
    public static class CompileCheck
    {
        [MenuItem("Tools/Compile Check")]
        public static void Run()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            CompilationPipeline.RequestScriptCompilation();
            EditorApplication.Exit(0);
        }
    }
}
