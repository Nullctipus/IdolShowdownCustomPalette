using System.Net;
using System.Collections.Generic;
using System.Collections;
using BepInEx;
using Newtonsoft.Json;
using UnityEngine;
using System.IO;
using HarmonyLib;
using System.Reflection;
using System;

namespace IdolShowdownCustomColorPalette;

[BepInPlugin(Plugin.GUID, Plugin.NAME, PluginInfo.PLUGIN_VERSION)]
public class EntryPoint : BaseUnityPlugin
{
    private void Awake()
    {
        GameObject gameobject = new GameObject(Plugin.NAME);
        DontDestroyOnLoad(gameobject);
        gameobject.hideFlags = HideFlags.HideAndDontSave;
        gameobject.AddComponent<Plugin>();
    }
}

public class Plugin : MonoBehaviour
{
    internal static Plugin Instance;
    public const string GUID = "org.apotheosis.colorswaps";
    public const string NAME = "Color Swapper";
    internal static BepInEx.Logging.ManualLogSource Logging => BepInEx.Logging.Logger.CreateLogSource("Palette Changer");
    private void OnDisable() {
        enabled = true;
    }
    Rect windowRect = new(500, 10, 700, 1000);
    KeyCode ToggleWindow = KeyCode.F4;
    bool DrawWindow = false;
    int windowID = GUID.GetHashCode();
    private void OnGUI()
    {
        if (DrawWindow)
            windowRect = GUI.Window(windowID, windowRect, DoWindow, NAME);
    }
    private void Update()
    {
        if (Input.GetKeyDown(ToggleWindow))
            DrawWindow ^= true;
    }
    IdolShowdown.Structs.Idol[] characters;
    IdolShowdown.Structs.Idol[] Characters
    {
        get
        {
            if (characters == null)
            {
                try
                {
                    characters = IdolShowdown.Managers.GlobalManager.Instance.GameManager.Characters;
                }
                catch { }
            }
            return characters;
        }
    }
    string _NewPaletteName;
    string EditPalette;
    int EditColour = -1;
    GUIStyle style;
    GUIStyle Style{
        get{
            if(style == null)
                style = GUI.skin.box;
            return style;
        }
        
    }
    string colorHex;
    static Color GetColor(string hex){
        Color ret = new();
        if(hex.StartsWith("#")) hex = hex.Substring(1);

        while(hex.Length<8)
            hex+="0";
        
        ret.r = int.Parse(hex.Substring(0,2), System.Globalization.NumberStyles.HexNumber)/255f;
        ret.g = int.Parse(hex.Substring(2,2), System.Globalization.NumberStyles.HexNumber)/255f;
        ret.b = int.Parse(hex.Substring(4,2), System.Globalization.NumberStyles.HexNumber)/255f;
        ret.a = int.Parse(hex.Substring(6,2), System.Globalization.NumberStyles.HexNumber)/255f;

        return ret;
    }
    static string GetHex(Color color){
        string hex = "#";
        hex += Mathf.CeilToInt(color.r*255).ToString("X2");
        hex += Mathf.CeilToInt(color.g*255).ToString("X2");
        hex += Mathf.CeilToInt(color.b*255).ToString("X2");
        hex += Mathf.CeilToInt(color.a*255).ToString("X2");

        return hex;
    }
    void DrawEditor(){
        GUILayout.BeginArea(new Rect(0, 40, 300, 950));
            if(GUILayout.Button("Save Palettes")){
                File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "../..", "IdolPalettes.json"), JsonConvert.SerializeObject(Palettes,Formatting.Indented));
            }
            GUILayout.BeginHorizontal();
                _NewPaletteName = GUILayout.TextField(_NewPaletteName);
                if(GUILayout.Button("Create Palette")){
                    if(!String.IsNullOrEmpty(_NewPaletteName))
                        NewPalette(_NewPaletteName);
                }
            GUILayout.EndHorizontal();
            foreach(var v in Palettes){
                if(GUILayout.Button(v.Key))
                    EditPalette = v.Key;
            }
        GUILayout.EndArea();
        GUILayout.BeginArea(new Rect(310, 40, 100, 950));
            if(!String.IsNullOrEmpty(EditPalette)){
                for(int i = 0; i<48;i++){
                    Style.active.textColor = GetColor(Palettes[EditPalette][i]);
                    if(GUILayout.Button("Colour "+i,style)){
                        EditColour = i;
                        colorHex = Palettes[EditPalette][i];
                    }
                }
            }
        GUILayout.EndArea();
        GUILayout.BeginArea(new Rect(420, 40, 280, 950));
            if(!String.IsNullOrEmpty(EditPalette) && EditColour != -1){
                GUILayout.BeginHorizontal();
                    colorHex = GUILayout.TextField(colorHex);
                    if(GUILayout.Button("Set")){
                        Palettes[EditPalette][EditColour] = colorHex;
                    }
                GUILayout.EndHorizontal();
            }
        GUILayout.EndArea();
    }
    string SelectedIdol;
    void DrawSelection(){
        if(Characters == null) return;

        GUILayout.BeginArea(new Rect(0, 40, 300, 950));
            foreach(var i in Characters){
                if(GUILayout.Button(i.charName, i.charName == SelectedIdol ? Style : GUI.skin.button))
                    SelectedIdol = i.charName;
                
            }
        GUILayout.EndArea();
        GUILayout.BeginArea(new Rect(310, 40, 390, 950));
            if(!String.IsNullOrEmpty(SelectedIdol)){
                if(SelectedPalette.ContainsKey(SelectedIdol) && GUILayout.Button("No Color Change"))
                    SelectedPalette.Remove(SelectedIdol);
                foreach(var v in Palettes)
                    if(GUILayout.Button(v.Key,(SelectedPalette.ContainsKey(SelectedIdol) && v.Key == SelectedPalette[SelectedIdol]) ? Style : GUI.skin.button))
                        {
                            if(SelectedPalette.ContainsKey(SelectedIdol))
                                SelectedPalette[SelectedIdol] = v.Key;
                            else
                                SelectedPalette.Add(SelectedIdol, v.Key);
                        }
            }
        GUILayout.EndArea();
    }
    int tab = 0;
    void DoWindow(int idx)
    {
        try{
        GUILayout.BeginHorizontal();
        if(GUILayout.Button("Selection")){
            tab = 0;
        }
        if(GUILayout.Button("Editor")){
            tab = 1;
        }
        GUILayout.EndHorizontal();
        if(tab == 0) DrawSelection();
        else DrawEditor();
        }catch(Exception e){
            Plugin.Logging.LogError(e);
        }
    }
    private void NewPalette(string name){
        if(Palettes.ContainsKey(name)) return;
        Palettes.Add(name,new());
        for(int i = 0;i<48;i++)
            Palettes[name].Add(i,"#FFFFFFFF");
        
    }
    private void Awake()
    {
        Instance = this;
        LoadPalettesFromDisk();
        HarmonyPatches.Init();
        

    }

    public static void ApplyPalette(string idol, SpriteRenderer renderer)
    {
        if (!SelectedPalette.ContainsKey(idol)) return;
        for (int i = 0; i < 48; i++)
        {
            renderer.material.SetColor("outColour" + i, GetColor(Palettes[SelectedPalette[idol]][i]));
        }
    }
    public static void ApplyPalette(IdolShowdown.Managers.GameCharacterManager.PlayerCharacter player)
    {
        ApplyPalette(player.idol.charName, player.charComponents.IdolSpriteRenderer);
    }
    static Dictionary<string, string> SelectedPalette = new();

    [SerializeField]
    static Dictionary<string, Dictionary<int, string>> Palettes;
    static void LoadPalettesFromDisk()
    {

        string path = Path.Combine(Application.streamingAssetsPath, "../..", "IdolPalettes.json");
        try
        {
            Palettes = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<int, string>>>(File.ReadAllText(path));
        }
        catch
        {
            Palettes = new();
            File.WriteAllText(path, JsonConvert.SerializeObject(Palettes,Formatting.Indented));
        }

    }

}
public static class HarmonyPatches
{
    internal static void Init()
    {
        harmony = new(Plugin.GUID);
        Plugin.Logging.LogInfo("Loading Harmony");

        // IdolShowdown.Managers.GameCharacterManager
        // private void ApplyCharacter(PlayerCharacter player)
        TryPatch(typeof(IdolShowdown.Managers.GameCharacterManager).GetMethod("ApplyCharacter", BindingFlags.NonPublic | BindingFlags.Instance), null, GetPatch(nameof(OnCharacterApply)));

        Plugin.Logging.LogInfo("Loaded Harmony");
    }
    internal static Harmony harmony;
    #region Helpers
    static bool TryPatch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, HarmonyMethod finalizer = null, HarmonyMethod ilmanipulator = null)
    {
        try
        {
            harmony.Patch(original, prefix, postfix, transpiler, finalizer, ilmanipulator);
        }
        catch (Exception e)
        {
            Plugin.Logging.LogError($"Problem patching {original.Name}:\n{e}");
            return false;
        }
        return true;
    }
    static HarmonyMethod GetPatch(string method)
    {
        return new HarmonyMethod(typeof(HarmonyPatches).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic));
    }
    #endregion

    private static void OnCharacterApply(IdolShowdown.Managers.GameCharacterManager.PlayerCharacter player)
    {
        Plugin.Instance.StartCoroutine(CharacterApply(player));
    }
    static IEnumerator CharacterApply(IdolShowdown.Managers.GameCharacterManager.PlayerCharacter player)
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        Plugin.ApplyPalette(player);

    }
}

