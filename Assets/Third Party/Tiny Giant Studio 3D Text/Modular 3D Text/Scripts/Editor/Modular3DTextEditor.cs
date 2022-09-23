using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace MText
{
    [CustomEditor(typeof(Modular3DText))]
    public class Modular3DTextEditor : Editor
    {
        Modular3DText myTarget;
        SerializedObject soTarget;

        SerializedProperty text;

        //main settings
        SerializedProperty font;
        SerializedProperty material;
        SerializedProperty fontSize;

        SerializedProperty characterSpacingInput;
        SerializedProperty lineSpacingInput;

        //positioning
        SerializedProperty height;
        SerializedProperty length;
        //SerializedProperty depth;

        //circular 
        SerializedProperty circularAlignmentRadius;
        SerializedProperty circularAlignmentSpreadAmount;
        SerializedProperty circularAlignmentAngle;

        //effects
        SerializedProperty typingEffects;
        SerializedProperty deletingEffects;
        SerializedProperty customDeleteAfterDuration;
        SerializedProperty deleteAfter;

        //advanced settings
        SerializedProperty repositionOldCharacters;
        SerializedProperty reApplyModulesToOldCharacters;
        //SerializedProperty activateChildObjects;

        SerializedProperty pool;
        SerializedProperty combineMeshInEditor;
        SerializedProperty dontCombineInEditorAnyway;
        SerializedProperty combineMeshDuringRuntime;
        SerializedProperty hideLettersInHierarchyInPlayMode;
        SerializedProperty updateTextOncePerFrame;
        SerializedProperty autoSaveMesh;

        SerializedProperty canBreakOutermostPrefab;

        SerializedProperty debugLogs;

        bool showCreationettings = false;
        bool showMainSettings = true;
        bool showModuleSettings = false;
        bool showAdvancedSettings = false;

        //style
        private static GUIStyle toggleStyle = null;
        private static GUIStyle foldOutStyle = null;



        string[] layoutOptions = new[] { "Linear layout", "Circular layout" };
        int layoutStyle = 0;




        Color openedFoldoutTitleColor = new Color(124 / 255f, 170 / 255f, 239 / 255f, 0.9f);
        Color toggledOnButtonColor = Color.white;
        Color toggledOffButtonColor = Color.grey;

        string addingtoolTip = "During runtime, these modules are called when new characters are added to the text.";
        string deleteingtoolTip = "During runtime, these modules are called when characters are removed from the text.";


        void OnEnable()
        {
            myTarget = (Modular3DText)target;
            soTarget = new SerializedObject(target);

            FindProperties();
            GetFoldoutSettings();

            layoutStyle = myTarget.LayoutStyle;
        }

        public override void OnInspectorGUI()
        {
            soTarget.Update();
            GenerateStyle();

            EditorGUI.BeginChangeCheck();

            WarningCheck();

            EditorGUILayout.PropertyField(text, GUIContent.none, GUILayout.Height(100));

            GUILayout.Space(5);

            MainSettings();

            GUILayout.Space(5);

            ModuleSettings();

            GUILayout.Space(5);

            AdvancedSettings();

            SaveFoldoutSettings();

            if (EditorGUI.EndChangeCheck())
            {
                if (layoutStyle != myTarget.LayoutStyle)
                    myTarget.LayoutStyle = layoutStyle;

                MText_Font font = myTarget.Font;

                if (soTarget.ApplyModifiedProperties())
                {
                    myTarget.UpdateText();
                }

                //In prefab mode font change wasn't updating for some reasons
                if (font != myTarget.Font)
                {
                    //Debug.Log("test");
                    myTarget.oldLineList.Clear();
                    myTarget.UpdateText();
                }

                //this is for people updating from old versions added 12/01/21. Will be removed after sufficient time has passed. Probably at 12/01/22
                if (!myTarget.GetComponent<MText_TextUpdater>())
                    myTarget.gameObject.AddComponent<MText_TextUpdater>();

                EditorUtility.SetDirty(myTarget);
            }
        }





        private void WarningCheck()
        {
            EditorGUI.indentLevel = 0;
            if (!myTarget.Font) EditorGUILayout.HelpBox("No font selected", MessageType.Error);
            if (!myTarget.Material) EditorGUILayout.HelpBox("No material selected", MessageType.Error);
            if (myTarget.DoesStyleInheritFromAParent()) EditorGUILayout.HelpBox("Some values are overwritten by parent button/list.", MessageType.Info);
        }



        #region Main Sections
        private void MainSettings()
        {
            GUILayout.BeginVertical("Box");
            EditorGUI.indentLevel = 1;

            GUILayout.BeginVertical(EditorStyles.toolbar);
            showMainSettings = EditorGUILayout.Foldout(showMainSettings, "Main Settings", true, foldOutStyle);
            GUILayout.EndVertical();

            if (showMainSettings)
            {
                //DrawUILine(blueFaded);
                EditorGUI.indentLevel = 1;

                GUILayout.Space(5);
                MText_Editor_Methods.HorizontalField(font, "Font", "", FieldSize.small);
                //if (!myTarget.DoesStyleInheritFromAParent())
                {
                    MText_Editor_Methods.HorizontalField(material, "Material", "", FieldSize.small);
                    MText_Editor_Methods.HorizontalField(fontSize, "Size", "", FieldSize.small);
                }

                EditorGUILayout.PropertyField(combineMeshInEditor, new GUIContent("Single mesh", "Combine into a single mesh in Editor, edit mode\n" +
                    "There is no reason to turn this on for playmode/build unless you really need this for something. \nOtherwise, wasted resource on combining\n" +
                    "Check advanced settings for more options"));

                EditorGUI.indentLevel = 2;
                DontCombineInEditorEither();
                EditorGUI.indentLevel = 0;

                GUILayout.Space(10);

                TextStyles();
                layoutStyle = EditorGUILayout.Popup(myTarget.LayoutStyle, layoutOptions, EditorStyles.popup, GUILayout.MinWidth(45));
                EditorGUI.indentLevel = 1;

                if (myTarget.LayoutStyle == 0)
                {
                    LinearAlignment();
                    GUILayout.Space(5);
                    LinearPositionSettings();
                    LinearSpacing();
                }
                else CircularLayoutSettings();
            }
            if (!Selection.activeTransform)
            {
                showMainSettings = false;
            }
            GUILayout.EndVertical();
        }
        private void ModuleSettings()
        {
            GUILayout.BeginVertical("Box");
            EditorGUI.indentLevel = 1;

            GUILayout.BeginVertical(EditorStyles.toolbar);
            showModuleSettings = EditorGUILayout.Foldout(showModuleSettings, new GUIContent("Modules", "Modules provide an easy way animate characters."), true, foldOutStyle);
            GUILayout.EndVertical();
            if (showModuleSettings)
            {
                EditorGUI.indentLevel = 2;

                ModuleContainerList("Adding", addingtoolTip, myTarget.typingEffects, typingEffects);
                GUILayout.Space(10);
                ModuleContainerList("Deleting", deleteingtoolTip, myTarget.deletingEffects, deletingEffects);
                GUILayout.Space(5);
                DeleteAfterDuration();
            }
            if (!Selection.activeTransform)
            {
                showModuleSettings = false;
            }
            GUILayout.EndVertical();
        }
        private void AdvancedSettings()
        {
            GUILayout.BeginVertical("Box");
            EditorGUI.indentLevel = 1;

            GUILayout.BeginVertical(EditorStyles.toolbar);
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true, foldOutStyle);
            GUILayout.EndVertical();
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel = 1;

                EditorGUILayout.PropertyField(pool, new GUIContent("Pooling", "Pooling massively increases performance if you are making a lot of changes"));
                EditorGUILayout.Space(10);
                CombineMeshSettings();
                EditorGUI.indentLevel = 1;

                EditorGUILayout.Space(10);
                //EditorGUILayout.PropertyField(repositionOldCharacters, new GUIContent("Reposition old Chars", "If old text = '123' and updated new text = '1234',\nthe '123' will be moved to their correct position when entering the '4'"));
                MText_Editor_Methods.HorizontalField(repositionOldCharacters, "Reposition old Chars", "If old text = '123' and updated new text = '1234',\nthe '123' will be moved to their correct position when entering the '4'", FieldSize.extraLarge);
                //EditorGUILayout.PropertyField(reApplyModulesToOldCharacters, new GUIContent("Re-apply modules", "If old text = old and updated new text = oldy,\ninstead of applying module to only 'y', it will apply to all chars"));
                MText_Editor_Methods.HorizontalField(reApplyModulesToOldCharacters, "Re-apply modules", "If old text = old and updated new text = oldy,\ninstead of applying module to only 'y', it will apply to all chars", FieldSize.extraLarge);
                //HorizontalFieldShortProperty(activateChildObjects, "Auto-activate ChildObjects", "", FieldSize.small);
                MText_Editor_Methods.HorizontalField(updateTextOncePerFrame, "Update once per frame", "If the gameobject is active in hierarchy, uses coroutine to make sure the text is only updated visually once per frame instead of wasting resources if updated multiple times by a script. This is only used if the game object is active in hierarchy and it updates at the end of frame.", FieldSize.extraLarge);
                EditorGUILayout.Space(10);


                GUIContent hideLetters = new GUIContent("Hide letters in Hierarchy", "Hides the game object of letters in the hierarchy. They are still there just not visible. No impact except for cleaner hierarchy.");
                EditorGUILayout.LabelField(hideLetters);
                EditorGUI.indentLevel = 3;
                MText_Editor_Methods.HorizontalField(hideLettersInHierarchyInPlayMode, "In play mode", "When turned on, the letters created during playmode won't show-up in hierarchy. Has no impact in usage other than clean hierarchy.", FieldSize.large);
                EditorGUI.indentLevel = 1;

                EditorGUILayout.Space(10);

                PrefabAdvancedSettings();


                EditorGUILayout.Space(10);
                EditorGUILayout.PropertyField(debugLogs);
            }
            if (!Selection.activeTransform)
            {
                showAdvancedSettings = false;
            }
            GUILayout.EndVertical();
            GUILayout.Space(15);
        }
        #endregion Main Sections


        #region Functions for main settings
        /// <summary>
        /// Direction, capitalize etc.
        /// </summary>
        private void TextStyles()
        {
            EditorGUILayout.BeginHorizontal();

            Color original = GUI.backgroundColor;

            GUIContent leftToRight = EditorGUIUtility.IconContent("tab_next@2x", "t");
            GUIContent rightToLeft = EditorGUIUtility.IconContent("tab_prev@2x");

            if (myTarget.textDirection == 1)
                GUI.backgroundColor = toggledOnButtonColor;
            else
                GUI.backgroundColor = toggledOffButtonColor;
            if (LeftButton(leftToRight))
            {
                myTarget.textDirection = 1;
                myTarget.UpdateText();
            }


            if (myTarget.textDirection == -1)
                GUI.backgroundColor = Color.white;
            else
                GUI.backgroundColor = toggledOffButtonColor;

            if (RightButton(rightToLeft))
            {
                myTarget.textDirection = -1;
                myTarget.UpdateText();
            }


            if (myTarget.LowerCase)
                GUI.backgroundColor = Color.white;
            else
                GUI.backgroundColor = toggledOffButtonColor;
            GUIContent smallCase = new GUIContent("ab", "lower case");
            if (LeftButton(smallCase))
            {
                myTarget.LowerCase = !myTarget.LowerCase;
                myTarget.Capitalize = false;
                myTarget.UpdateText();
                EditorUtility.SetDirty(myTarget);
            }


            if (myTarget.Capitalize)
                GUI.backgroundColor = Color.white;
            else
                GUI.backgroundColor = toggledOffButtonColor;

            GUIContent capitalize = new GUIContent("AB", "UPPER CASE");
            if (RightButton(capitalize))
            {
                myTarget.Capitalize = !myTarget.Capitalize;
                myTarget.LowerCase = false;
                myTarget.UpdateText();
                EditorUtility.SetDirty(myTarget);
            }

            GUI.backgroundColor = original;
            EditorGUILayout.EndHorizontal();
        }
        void LinearPositionSettings()
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Height", GUILayout.MaxWidth(60));
            EditorGUILayout.PropertyField(height, GUIContent.none, GUILayout.MaxWidth(50));
            EditorGUILayout.LabelField("Width", GUILayout.MaxWidth(60));
            EditorGUILayout.PropertyField(length, GUIContent.none, GUILayout.MaxWidth(50));

            GUILayout.EndHorizontal();
        }
        void LinearSpacing()
        {
            GUILayout.Space(5);

            EditorGUILayout.LabelField("Spacing");

            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("", GUILayout.MaxWidth(10));
            EditorGUILayout.LabelField("Character", GUILayout.MaxWidth(75));
            EditorGUILayout.PropertyField(characterSpacingInput, GUIContent.none, GUILayout.MaxWidth(40));
            EditorGUILayout.LabelField("Line", GUILayout.MaxWidth(55));
            EditorGUILayout.PropertyField(lineSpacingInput, GUIContent.none, GUILayout.MaxWidth(40));

            GUILayout.EndHorizontal();

            GUILayout.Space(5);
        }
        void LinearAlignment()
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            HorizontalAlignment();
            VerticalAlignment();
            EditorGUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
        void CircularLayoutSettings()
        {
            MText_Editor_Methods.HorizontalField(circularAlignmentRadius, "Radius", "", FieldSize.small);
            MText_Editor_Methods.HorizontalField(circularAlignmentSpreadAmount, "Spread", "", FieldSize.small);
            MText_Editor_Methods.HorizontalField(circularAlignmentAngle, "Angle", "", FieldSize.small);

            LinearSpacing();
        }
        void HorizontalAlignment()
        {
            EditorGUILayout.BeginHorizontal();

            Color originalColor = GUI.backgroundColor;

            GUIContent horizontallyleftIcon = EditorGUIUtility.IconContent("align_horizontally_left");
            GUIContent horizontallyCenterIcon = EditorGUIUtility.IconContent("align_horizontally_center");
            GUIContent horizontallyRightIcon = EditorGUIUtility.IconContent("align_horizontally_right");

            if (myTarget.alignLeft)
                GUI.backgroundColor = toggledOnButtonColor;
            else
                GUI.backgroundColor = toggledOffButtonColor;

            if (LeftButton(horizontallyleftIcon))
            {
                myTarget.alignLeft = true;
                myTarget.alignRight = false;
                myTarget.alignCenter = false;
                //if (myTarget.autoCreateInEditor) 
                myTarget.UpdateText();
            }
            if (myTarget.alignCenter)
                GUI.backgroundColor = toggledOnButtonColor;
            else
                GUI.backgroundColor = toggledOffButtonColor;

            if (MidButton(horizontallyCenterIcon))
            {
                myTarget.alignCenter = true;
                myTarget.alignRight = false;
                myTarget.alignLeft = false;
                //if (myTarget.autoCreateInEditor) 
                myTarget.UpdateText();
            }
            if (myTarget.alignRight)
                GUI.backgroundColor = toggledOnButtonColor;
            else
                GUI.backgroundColor = toggledOffButtonColor;

            if (RightButton(horizontallyRightIcon))
            {
                myTarget.alignLeft = false;
                myTarget.alignCenter = false;
                myTarget.alignRight = true;
                //if (myTarget.autoCreateInEditor) 
                myTarget.UpdateText();
            }

            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndHorizontal();
        }
        void VerticalAlignment()
        {
            Color orignalColor = GUI.backgroundColor;

            EditorGUILayout.BeginHorizontal();

            GUIContent verticallyTopIcon = EditorGUIUtility.IconContent("align_vertically_top");
            GUIContent verticallyMiddleIcon = EditorGUIUtility.IconContent("align_vertically_center");
            GUIContent verticallyBottomIcon = EditorGUIUtility.IconContent("align_vertically_bottom");

            if (myTarget.alignTop)
                GUI.backgroundColor = toggledOnButtonColor;
            else
                GUI.backgroundColor = toggledOffButtonColor;

            if (LeftButton(verticallyTopIcon))
            {
                myTarget.alignTop = true;
                myTarget.alignMiddle = false;
                myTarget.alignBottom = false;
                //if (myTarget.autoCreateInEditor) 
                myTarget.UpdateText();
            }

            if (myTarget.alignMiddle)
                GUI.backgroundColor = toggledOnButtonColor;
            else
                GUI.backgroundColor = toggledOffButtonColor;

            if (MidButton(verticallyMiddleIcon))
            {
                myTarget.alignTop = false;
                myTarget.alignMiddle = true;
                myTarget.alignBottom = false;
                //if (myTarget.autoCreateInEditor) 
                myTarget.UpdateText();
            }

            if (myTarget.alignBottom)
                GUI.backgroundColor = toggledOnButtonColor;
            else
                GUI.backgroundColor = toggledOffButtonColor;
            if (RightButton(verticallyBottomIcon))
            {
                myTarget.alignTop = false;
                myTarget.alignMiddle = false;
                myTarget.alignBottom = true;
                //if (myTarget.autoCreateInEditor) 
                myTarget.UpdateText();
            }
            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = orignalColor;
        }
        #endregion Functions for main settings


        #region Functions for module settings
        private void DeleteAfterDuration()
        {
            string toolTip = "Determines when a character is removed from text, how long it takes to be deleted. \nIf set to false, when a character is deleted, it is removed instantly or after the highest duration of modules, if there is any.";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(customDeleteAfterDuration, GUIContent.none, GUILayout.MaxWidth(15));
            if (!myTarget.customDeleteAfterDuration)
            {
                float duration = GetDeleteDuration();
                GUIContent content = new GUIContent("Delete After : " + duration + " seconds", toolTip);
                EditorGUILayout.LabelField(content);
            }
            else
            {
                MText_Editor_Methods.HorizontalField(deleteAfter, "Delete After", "", FieldSize.small);
                GUIContent content = new GUIContent(" seconds");
                EditorGUILayout.LabelField(content);
            }
            EditorGUILayout.EndHorizontal();
        }
        private float GetDeleteDuration()
        {
            float max = 0;
            for (int i = 0; i < myTarget.deletingEffects.Count; i++)
            {
                if (myTarget.deletingEffects[i].duration > max)
                    max = myTarget.deletingEffects[i].duration;
            }
            return max;
        }
        private void ModuleContainerList(string label, string tooltip, List<MText_ModuleContainer> moduleContainers, SerializedProperty serializedContainer)
        {
            Color original = GUI.backgroundColor;
            Color originalContent = GUI.contentColor;


            GUILayout.BeginVertical("Box");
            EditorGUI.indentLevel = 0;

            GUILayout.BeginVertical(EditorStyles.toolbar);
            EditorGUILayout.LabelField(new GUIContent(label, tooltip));
            GUILayout.EndVertical();

            GUILayout.Space(5);

            if (moduleContainers.Count > 0)
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.MaxWidth(2));
                EditorGUILayout.LabelField("Module", EditorStyles.miniLabel, GUILayout.MinWidth(10));
                EditorGUILayout.LabelField(new GUIContent("Duration", "The value passed to the module. It is in mostly the duration in built-in modules but can be anything. Please check individual modules to see what this does."), EditorStyles.miniLabel, GUILayout.MaxWidth(65));
                EditorGUILayout.LabelField("", GUILayout.MaxWidth(30));

                GUILayout.EndHorizontal();
            }

            GUI.backgroundColor = Color.white;
            GUI.contentColor = originalContent;
            GUIContent deleteIcon = EditorGUIUtility.IconContent("d_winbtn_win_close");

            for (int i = 0; i < moduleContainers.Count; i++)
            {
                GUILayout.BeginVertical("Box");
                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedContainer.GetArrayElementAtIndex(i).FindPropertyRelative("module"), GUIContent.none, GUILayout.MinWidth(10));
                EditorGUILayout.PropertyField(serializedContainer.GetArrayElementAtIndex(i).FindPropertyRelative("duration"), GUIContent.none, GUILayout.MaxWidth(65));

                if (GUILayout.Button(deleteIcon, GUILayout.MinHeight(20), GUILayout.MaxWidth(20)))
                {
                    moduleContainers.RemoveAt(i);
                }
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            GUI.contentColor = Color.black;
            //GUIContent addIcon = EditorGUIUtility.IconContent("d_TreeEditor.Trash");
            if (GUILayout.Button("Add New Module", GUILayout.MinHeight(20)))
            {
                myTarget.EmptyEffect(moduleContainers);
            }

            GUI.contentColor = originalContent;

            GUILayout.EndVertical();
            GUI.backgroundColor = original;
        }
        #endregion Functions for module settings


        #region Functions for advanced settings        
        private void PrefabAdvancedSettings()
        {
            if (PrefabUtility.IsPartOfPrefabInstance(myTarget.gameObject))
            {
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(myTarget.gameObject))
                {
                    MText_Editor_Methods.HorizontalField(canBreakOutermostPrefab, "Break outermost Prefab", "If the text isn't a child object of the prefab, it can break prefab and save the reference.", FieldSize.extraLarge);
                }
            }
            else MeshSaveSettings();
            PrefabMeshSaveSettings();
        }
        private void PrefabMeshSaveSettings()
        {
            if (myTarget.assetPath != "" && myTarget.assetPath != null && !EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField(myTarget.assetPath, EditorStyles.boldLabel);
                if (GUILayout.Button("Apply to prefab"))
                {
                    myTarget.ReconnectPrefabs();
                }
            }

            if ((myTarget.assetPath != "" && myTarget.assetPath != null && !EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Remove prefab connection"))
                {
                    myTarget.assetPath = "";
                }
            }
            if (PrefabUtility.IsPartOfPrefabInstance(myTarget.gameObject))
            {
                MeshSaveSettings();
            }
        }
        #endregion Functions for advanced settings


        private void CombineMeshSettings()
        {
            EditorGUILayout.LabelField(new GUIContent("Single mesh", "Combines character meshes" +
                "\nUses unity's Mesh.Combine method.\n" +
                "Unity has a limit of verticies one mesh can have which causes the bugs on large texts"));
            EditorGUI.indentLevel = 3;
            EditorGUILayout.PropertyField(combineMeshDuringRuntime, new GUIContent("In Play mode", "There is no reason to turn this on unless you really need this for something. \nOtherwise, wasted resource on combining"));
            EditorGUILayout.PropertyField(combineMeshInEditor, new GUIContent("In Editor", "The same option as found in Main Settings"));
            EditorGUI.indentLevel = 4;

            DontCombineInEditorEither();

            if (myTarget.gameObject.GetComponent<MeshFilter>())
            {
                if (GUILayout.Button(new GUIContent("Optimize mesh", "This causes the geometry and vertices of the combined mesh to be reordered internally in an attempt to improve vertex cache utilisation on the graphics hardware and thus rendering performance. This operation can take a few seconds or more for complex meshes.")))
                {
                    MText_Utilities.OptimizeMesh(myTarget.gameObject.GetComponent<MeshFilter>().sharedMesh);
                }
            }
        }
        private void MeshSaveSettings()
        {
            if (myTarget.gameObject.GetComponent<MeshFilter>())
            {
                EditorGUILayout.PropertyField(autoSaveMesh);

                GUILayout.BeginHorizontal();
                if (!myTarget.autoSaveMesh)
                {
                    if (GUILayout.Button(new GUIContent("Save mesh", "PREFABS need saved meshes")))
                    {
                        myTarget.SaveMeshAsAsset(false);
                    }
                }
                if (GUILayout.Button(new GUIContent("Save mesh as", "Save a new copy of the mesh in project")))
                {
                    myTarget.SaveMeshAsAsset(true);
                }
                GUILayout.EndHorizontal();
            }
        }
        private void DontCombineInEditorEither()
        {
            if (!myTarget.combineMeshInEditor && PrefabUtility.IsPartOfPrefabInstance(myTarget.gameObject))
            {
                //GUIContent helpIcon = EditorGUIUtility.IconContent("console.warnicon");
                GUILayout.BeginHorizontal();
                string warning = "Prefabs don't allow child objects that are part of the prefab to be deleted in Editor.\n" +
                    "If you add child objects, then apply, which adds these child objects to the prefab,\n" +
                    "When changing text again, this script can't delete the old gameobjects. Just disables them. Remember to clean them up manually if you enable this.";
                EditorGUILayout.PropertyField(dontCombineInEditorAnyway, new GUIContent("In Prefab", warning), GUILayout.MinWidth(250));
                GUILayout.EndHorizontal();
            }
        }




        //private void HorizontalField(SerializedProperty property, string label, string tooltip = "", FieldSize fieldSize = FieldSize.normal)
        //{
        //    float myMaxWidth = 100;
        //    //not to self: it's ternary operator not tarnary operator. Stop mistyping
        //    if (settings)
        //        myMaxWidth = fieldSize == FieldSize.small ? settings.smallHorizontalFieldSize : fieldSize == FieldSize.normal ? settings.normalltHorizontalFieldSize : fieldSize == FieldSize.large ? settings.largeHorizontalFieldSize : fieldSize == FieldSize.extraLarge ? settings.extraLargeHorizontalFieldSize : settings.normalltHorizontalFieldSize;
        //    else
        //        myMaxWidth = fieldSize == FieldSize.small ? defaultSmallHorizontalFieldSize : fieldSize == FieldSize.normal ? defaultNormalltHorizontalFieldSize : fieldSize == FieldSize.large ? defaultLargeHorizontalFieldSize : fieldSize == FieldSize.extraLarge ? defaultExtraLargeHorizontalFieldSize : defaultNormalltHorizontalFieldSize;

        //    GUILayout.BeginHorizontal();
        //    GUIContent gUIContent = new GUIContent(label, tooltip);
        //    EditorGUILayout.LabelField(gUIContent, GUILayout.MaxWidth(myMaxWidth));
        //    EditorGUILayout.PropertyField(property, GUIContent.none);
        //    GUILayout.EndHorizontal();
        //}


        void DrawUILine(Color color, int thickness = 1, int padding = 0)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        #region Style
        void GenerateStyle()
        {
            if (toggleStyle == null)
            {
                toggleStyle = new GUIStyle(GUI.skin.button);
                toggleStyle.margin = new RectOffset(0, 0, toggleStyle.margin.top, toggleStyle.margin.bottom);
            }

            if (foldOutStyle == null)
            {
                //foldOutStyle = new GUIStyle(EditorStyles.foldout);
                foldOutStyle = new GUIStyle(EditorStyles.foldout);
                //foldOutStyle.overflow = new RectOffset(-10, 0, 3, 0);
                foldOutStyle.padding = new RectOffset(14, 0, 0, 0);

                foldOutStyle.onNormal.textColor = openedFoldoutTitleColor;
            }


            EditorStyles.popup.fontSize = 11;
            EditorStyles.popup.fixedHeight = 18;
        }
        bool LeftButton(GUIContent content)
        {
            bool clicked = false;
            Rect rect = GUILayoutUtility.GetRect(20, 17);

            GUI.BeginGroup(rect);
            if (GUI.Button(new Rect(0, 0, rect.width + toggleStyle.border.right, rect.height), content, toggleStyle))
                clicked = true;

            GUI.EndGroup();
            return clicked;
        }
        bool MidButton(GUIContent content)
        {
            bool clicked = false;
            Rect rect = GUILayoutUtility.GetRect(20, 17);


            GUI.BeginGroup(rect);
            if (GUI.Button(new Rect(-toggleStyle.border.left, 0, rect.width + toggleStyle.border.left + toggleStyle.border.right, rect.height), content, toggleStyle))
                clicked = true;
            GUI.EndGroup();
            return clicked;
        }
        bool RightButton(GUIContent content)
        {
            bool clicked = false;
            Rect rect = GUILayoutUtility.GetRect(20, 17);


            GUI.BeginGroup(rect);
            if (GUI.Button(new Rect(-toggleStyle.border.left, 0, rect.width + toggleStyle.border.left, rect.height), content, toggleStyle))
                clicked = true;
            GUI.EndGroup();
            return clicked;
        }
        #endregion

        #region Functions
        /// <summary>
        /// Called on Enable
        /// </summary>
        private void FindProperties()
        {
            text = soTarget.FindProperty("_text");

            autoSaveMesh = soTarget.FindProperty("autoSaveMesh");

            //main settings
            font = soTarget.FindProperty("_font");
            material = soTarget.FindProperty("_material");
            fontSize = soTarget.FindProperty("_fontSize");

            characterSpacingInput = soTarget.FindProperty("_characterSpacing");
            lineSpacingInput = soTarget.FindProperty("_lineSpacing");

            //position settings
            height = soTarget.FindProperty("_height");
            length = soTarget.FindProperty("_width");
            //depth = soTarget.FindProperty("depth");

            //circular Layout
            circularAlignmentRadius = soTarget.FindProperty("_circularAlignmentRadius");
            circularAlignmentSpreadAmount = soTarget.FindProperty("_circularAlignmentSpreadAmount");
            circularAlignmentAngle = soTarget.FindProperty("_circularAlignmentAngle");

            //effects
            typingEffects = soTarget.FindProperty("typingEffects");
            deletingEffects = soTarget.FindProperty("deletingEffects");
            customDeleteAfterDuration = soTarget.FindProperty("customDeleteAfterDuration");
            deleteAfter = soTarget.FindProperty("deleteAfter");

            //advanced
            repositionOldCharacters = soTarget.FindProperty("repositionOldCharacters");
            reApplyModulesToOldCharacters = soTarget.FindProperty("reApplyModulesToOldCharacters");
            //activateChildObjects = soTarget.FindProperty("activateChildObjects");

            pool = soTarget.FindProperty("pooling");
            combineMeshInEditor = soTarget.FindProperty("combineMeshInEditor");
            dontCombineInEditorAnyway = soTarget.FindProperty("dontCombineInEditorAnyway");
            combineMeshDuringRuntime = soTarget.FindProperty("combineMeshDuringRuntime");
            hideLettersInHierarchyInPlayMode = soTarget.FindProperty("hideLettersInHierarchyInPlayMode");
            //hideLettersInHierarchyInEditMode = soTarget.FindProperty("hideLettersInHierarchyInEditMode");
            updateTextOncePerFrame = soTarget.FindProperty("updateTextOncePerFrame");


            canBreakOutermostPrefab = soTarget.FindProperty("canBreakOutermostPrefab");
            debugLogs = soTarget.FindProperty("debugLogs");
        }

        /// <summary>
        /// Called on Enable
        /// </summary>
        private void GetFoldoutSettings()
        {
            showCreationettings = myTarget.showCreationettingsInEditor;
            showMainSettings = myTarget.showMainSettingsInEditor;
            showModuleSettings = myTarget.showModuleSettingsInEditor;
            showAdvancedSettings = myTarget.showAdvancedSettingsInEditor;
        }

        /// <summary>
        /// Called to save the settings to script, so that they will be remembered
        /// </summary>
        private void SaveFoldoutSettings()
        {
            myTarget.showCreationettingsInEditor = showCreationettings;
            myTarget.showMainSettingsInEditor = showMainSettings;
            myTarget.showModuleSettingsInEditor = showModuleSettings;
            myTarget.showAdvancedSettingsInEditor = showAdvancedSettings;
        }
        #endregion Functions
    }
}