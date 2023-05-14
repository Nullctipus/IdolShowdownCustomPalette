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
    Rect windowRect = new(500, 10, 2000, 1000);
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
                style = new(GUI.skin.box);
                style.normal.background = Texture2D.whiteTexture;
            return style;
        }
        
    }
    string colorHex;
    static Color GetColor(string hex){
        if(hex == "NONE") return Color.white;
        
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
        //GUILayout.BeginArea(new Rect(0, 40, 300, 30*(Palettes.Count+2)));
        
        scroll = GUI.BeginScrollView(new Rect(0, 40, 300, 950),scroll,new Rect(0, 0, 300, 25*(Palettes.Count+2)),false,false,GUIStyle.none,GUI.skin.verticalScrollbar);
            if(GUI.Button(new Rect(0,0,280,20),"Save Palettes")){
                File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "../..", "IdolPalettes.json"), JsonConvert.SerializeObject(Palettes,Formatting.Indented));
            }
                _NewPaletteName = GUI.TextField(new Rect(0,25,180,20),_NewPaletteName);
                if(GUI.Button(new Rect(180,25,100,20),"Create Palette")){
                    if(!String.IsNullOrEmpty(_NewPaletteName))
                        NewPalette(_NewPaletteName);
                }
                int j = 1;
            foreach(var v in Palettes){
                if(GUI.Button(new Rect(0,++j*25,280,20),v.Key))
                    EditPalette = v.Key;
            }
        GUI.EndScrollView();
        //GUILayout.EndArea();
        //GUILayout.BeginArea(new Rect(310, 40, 100, 30*49));
        GUI.BeginScrollView(new Rect(310, 40, 120, 950),scroll,new Rect(0,0,120,30*49),false,false,GUIStyle.none,GUIStyle.none);
            if(!String.IsNullOrEmpty(EditPalette))
            {
                Color last = GUI.backgroundColor;
                for(int i = 0; i<48;i++){
                    GUI.backgroundColor = GetColor(Palettes[EditPalette][i]);
                    if(GUI.Button(new Rect(0,25*i,100,20),"Colour "+i,Style)){
                        EditColour = i;
                        colorHex = Palettes[EditPalette][i];
                    }
                }
                GUI.backgroundColor = last;
            }
        GUI.EndScrollView();
        //GUILayout.EndArea();
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
    Vector2 scroll = Vector2.zero;
    Vector2 scrollPosition = Vector2.zero;
    void DrawSelection(){
        if(Characters == null) return;

        GUILayout.BeginArea(new Rect(0, 40, 300, 950));
            foreach(var i in Characters){
                if(GUILayout.Button(i.charName, i.charName == SelectedIdol ? GUI.skin.box : GUI.skin.button))
                    SelectedIdol = i.charName;
            }
        GUILayout.EndArea();
        //GUILayout.BeginArea(new Rect(310, 40, 390, 30*(Palettes.Count+1)));
        if(!String.IsNullOrEmpty(SelectedIdol)){
            scroll = GUI.BeginScrollView(new Rect(310, 40, 220, 980),scroll,new Rect(0, 0, 220, 25*(Palettes.Count+1)),false,false,GUIStyle.none,GUI.skin.verticalScrollbar);
            
                if(SelectedPalette.ContainsKey(SelectedIdol) && GUI.Button(new Rect(0,0,200,20),"No Color Change"))
                    SelectedPalette.Remove(SelectedIdol);
                    int i = 1;
                foreach(var v in Palettes)
                    if(GUI.Button(new Rect(0,25*i++,200,20),v.Key,(SelectedPalette.ContainsKey(SelectedIdol) && v.Key == SelectedPalette[SelectedIdol]) ? GUI.skin.box : GUI.skin.button))
                        {
                            if(SelectedPalette.ContainsKey(SelectedIdol))
                                SelectedPalette[SelectedIdol] = v.Key;
                            else
                                SelectedPalette.Add(SelectedIdol, v.Key);
                        }
            
            GUI.EndScrollView();
        }
        //GUILayout.EndArea();
    }
    int tab = 0;
    void DoWindow(int idx)
    {
        try
        {
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Selection")){
                tab = 0;
            }
            if(GUILayout.Button("Editor")){
                tab = 1;
            }
            if(GUILayout.Button("Extract Default"))
                ExportDefaultPalettes();
            GUILayout.EndHorizontal();
            
                if(tab == 0) DrawSelection();
                else DrawEditor();
        }
        catch(Exception e)
        {
            Plugin.Logging.LogError(e);
        }
    }
    private void NewPalette(string name)
    {
        if(Palettes.ContainsKey(name)) return;
        Palettes.Add(name,new string[48]);
        for(int i = 0;i<48;i++)
            Palettes[name][i] = "NONE";
        
    }
    private void Awake()
    {
        Instance = this;
        LoadPalettesFromDisk();
        HarmonyPatches.Init();
        

    } 
    public void ExportDefaultPalettes()
    {
        if(Characters == null) return;

        foreach(var c in characters){
            for(int p =0;p<c.paletteSwapMaterials.Count;p++){
                if(!Palettes.ContainsKey(c.charName+p)) Palettes.Add(c.charName+p,new string[48]);
                for(int i = 0;i<48;i++){
                    if(c.charName == "Coco" && p == 0)// Coco's First color is bugged
                        Palettes["Coco0"][i] = GetHex(c.paletteSwapMaterials[1].GetColor("inColour"+i)); 
                    else
                        Palettes[c.charName+p][i] = GetHex(c.paletteSwapMaterials[p].GetColor("outColour"+i)); 
                }
                    
            }
        }
    }

    public static void ApplyPalette(string idol, SpriteRenderer renderer)
    {
        if (!SelectedPalette.ContainsKey(idol)) return;
        for (int i = 0; i < 48; i++)
        {
            if(idol == "Coco" && Palettes.ContainsKey("Coco0"))
                renderer.material.SetColor("inColour" + i, GetColor(Palettes["Coco0"][i]));
            if(Palettes[SelectedPalette[idol]][i] != "NONE")
                renderer.material.SetColor("outColour" + i, GetColor(Palettes[SelectedPalette[idol]][i]));
        }
    }
    public static void ApplyPalette(IdolShowdown.Managers.GameCharacterManager.PlayerCharacter player)
    {
        ApplyPalette(player.idol.charName, player.charComponents.IdolSpriteRenderer);
    }
    static Dictionary<string, string> SelectedPalette = new();

    [SerializeField]
    static Dictionary<string,  string[]> Palettes;
    static void LoadPalettesFromDisk()
    {

        string path = Path.Combine(Application.streamingAssetsPath, "../..", "IdolPalettes.json");
        try
        {
            Palettes = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(File.ReadAllText(path));
        }
        catch(Exception e)
        {
            Logging.LogError(e);
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

