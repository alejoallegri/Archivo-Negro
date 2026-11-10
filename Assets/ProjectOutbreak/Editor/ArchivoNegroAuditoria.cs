#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Diagnóstico de dependencias del proyecto. No borra, mueve ni renombra assets.
/// Las referencias cargadas dinámicamente por código pueden no figurar.
/// </summary>
public static class ArchivoNegroAuditoria
{
    private const string FinalScene = "Assets/ProjectOutbreak/Scenes/ArchivoNegroBiblioteca.unity";
    private const string ReportName = "auditoria_archivo_negro.txt";

    [MenuItem("Herramientas/Archivo Negro/Auditar archivos sin borrar")]
    public static void Generate()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(FinalScene) == null)
        {
            EditorUtility.DisplayDialog("Falta la escena final",
                "No encontré " + FinalScene + ". Guardá la última escena primero.", "Entendido");
            return;
        }

        var dependencies = new HashSet<string>(
            AssetDatabase.GetDependencies(FinalScene, true), StringComparer.OrdinalIgnoreCase);
        dependencies.Add(FinalScene);

        var allPaths = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) && File.Exists(p))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var unused = allPaths.Where(p => !dependencies.Contains(p)).ToList();
        var active = allPaths.Where(dependencies.Contains).ToList();
        var protectedPaths = unused.Where(IsSpecial).ToList();
        var candidates = unused.Where(p => !IsSpecial(p))
            .OrderByDescending(GetSize).ToList();

        long allSize = allPaths.Sum(GetSize);
        long candidateSize = candidates.Sum(GetSize);
        var sb = new StringBuilder(120000);
        sb.AppendLine("ARCHIVO NEGRO - INFORME DE DEPENDENCIAS SIN BORRADOS");
        sb.AppendLine("==================================================");
        sb.AppendLine("Escena analizada: " + FinalScene);
        sb.AppendLine("Fecha: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("ATENCION: no elimina nada. 'No referenciado' NO significa que sea seguro borrar.");
        sb.AppendLine("Los recursos cargados mediante Resources.Load, Shader.Find, Addressables,");
        sb.AppendLine("AssetDatabase, configuraciones globales o codigo no siempre figuran.");
        sb.AppendLine();
        sb.AppendLine("RESUMEN");
        sb.AppendLine("Archivos analizados: " + allPaths.Count);
        sb.AppendLine("Referenciados por la escena: " + active.Count);
        sb.AppendLine("Sin referencia directa/recursiva: " + unused.Count);
        sb.AppendLine("Protegidos para revision manual: " + protectedPaths.Count);
        sb.AppendLine("Candidatos para evaluar: " + candidates.Count);
        sb.AppendLine("Tamaño total assets: " + Size(allSize));
        sb.AppendLine("Tamaño potencial de candidatos: " + Size(candidateSize));
        sb.AppendLine();

        Section(sb, "ESCENAS Y SU ESTADO");
        foreach (var p in allPaths.Where(p => p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)))
            sb.AppendLine((dependencies.Contains(p) ? "EN USO  " : "REVISAR " ) + Size(GetSize(p)).PadLeft(11) + "  " + p);

        Section(sb, "ARCHIVOS .BLEND EN REALTEXTURES");
        foreach (var p in allPaths.Where(p => p.EndsWith(".blend", StringComparison.OrdinalIgnoreCase)))
            sb.AppendLine((dependencies.Contains(p) ? "REFERENCIADO " : "NO REFERENCIADO ") +
                Size(GetSize(p)).PadLeft(10) + "  " + p);

        Section(sb, "SCRIPTS INSTALADORES EN EDITOR");
        foreach (var p in allPaths.Where(p => p.Contains("/ProjectOutbreak/Editor/")))
            sb.AppendLine((dependencies.Contains(p) ? "REFERENCIADO " : "NO REFERENCIADO ") + p);

        Section(sb, "CANDIDATOS NO REFERENCIADOS - MAYORES PRIMERO");
        foreach (var p in candidates)
            sb.AppendLine(Size(GetSize(p)).PadLeft(12) + "  " + p);

        Section(sb, "NO REFERENCIADOS PERO PROTEGIDOS PARA REVISION");
        foreach (var p in protectedPaths)
            sb.AppendLine(Size(GetSize(p)).PadLeft(12) + "  " + p);

        Section(sb, "REFERENCIAS CONFIRMADAS POR LA ESCENA FINAL");
        foreach (var p in active)
            sb.AppendLine(Size(GetSize(p)).PadLeft(12) + "  " + p);

        string reportPath = Path.Combine(Directory.GetCurrentDirectory(), ReportName);
        File.WriteAllText(reportPath, sb.ToString(), new UTF8Encoding(false));
        Debug.Log("Auditoría generada: " + reportPath + " (sin modificar Assets).");
        EditorUtility.DisplayDialog("Auditoría terminada",
            "Se creó " + ReportName + " junto a las carpetas Assets y Packages. " +
            "Mandá ese TXT para revisar qué se puede retirar. No se borró nada.", "Listo");
        EditorUtility.RevealInFinder(reportPath);
    }

    private static bool IsSpecial(string path)
    {
        // Las referencias de scripts, configuraciones y recursos dinámicos
        // requieren una verificación adicional antes de cualquier eliminación.
        return path.StartsWith("Assets/Starter Assets/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("Assets/Settings/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("Assets/Easy FPS/Resources/", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".inputactions", StringComparison.OrdinalIgnoreCase);
    }

    private static long GetSize(string relativePath)
    {
        try { return new FileInfo(relativePath).Length; }
        catch { return 0; }
    }

    private static string Size(long bytes)
    {
        if (bytes >= 1024L * 1024 * 1024) return (bytes / (1024d * 1024 * 1024)).ToString("0.00") + " GiB";
        if (bytes >= 1024 * 1024) return (bytes / (1024d * 1024)).ToString("0.00") + " MiB";
        if (bytes >= 1024) return (bytes / 1024d).ToString("0.0") + " KiB";
        return bytes + " B";
    }

    private static void Section(StringBuilder sb, string title)
    {
        sb.AppendLine();
        sb.AppendLine("==================================================");
        sb.AppendLine(title);
        sb.AppendLine("==================================================");
    }
}
#endif
