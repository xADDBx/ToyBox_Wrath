﻿using HarmonyLib;
using Kingmaker.Blueprints;
using ModKit;
using Owlcat.Runtime.Visual.Overrides.HBAO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.VolumeComponent;

namespace ToyBox.PatchTool; 
public static class PatchToolUI {
    public static PatchState CurrentState;
    private static Dictionary<(object, FieldInfo), object> _editStates = new();
    private static Dictionary<object, Dictionary<FieldInfo, object>> _fieldsByObject = new();
    // key: parent, containing field, object instance
    private static Dictionary<(object, FieldInfo, object), bool> _toggleStates = new();
    private static Dictionary<((object, FieldInfo), int), bool> _listToggleStates = new();
    private static HashSet<object> _visited = new();
    // private static string _target = "649ae43543fd4b47ae09a6547e67bcfc";
    private static string _target = "";
    private static string _pickerText = "";
    private static readonly HashSet<Type> _primitiveTypes = [typeof(string), typeof(bool), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double)];
    public static void SetTarget(string guid) {
        CurrentState = null;
        ClearCache();
        _target = guid;
    }
    public static void OnGUI() {
        _visited.Clear();
        using (HorizontalScope()) {
            Label("Enter target blueprint id", Width(200));
            TextField(ref _pickerText, null, Width(350));
            ActionButton("Pick Blueprint", () => {
                SetTarget(_pickerText);
            });
        }
        Div();
        if (CurrentState == null || CurrentState.IsDirty && !_target.IsNullOrEmpty()) {
            if (Event.current.type == EventType.Layout) {
                ClearCache();
                var bp = ResourcesLibrary.TryGetBlueprint(_target);
                if (bp != null) {
                    CurrentState = new(bp);
                }
            }
        }
        if (CurrentState != null) {
            NestedGUI(CurrentState.Blueprint);
        }
    }
    public static void ClearCache() {
        _editStates.Clear();
        _fieldsByObject.Clear();
        _toggleStates.Clear();
    }

    public static void NestedGUI(object o, PatchOperation wouldBePatch = null, int indent = 0) {
        if (_visited.Contains(o)) {
            Label("Already opened on another level!".Green());
            return;
        }
        _visited.Add(o);
        if (!_fieldsByObject.ContainsKey(o)) {
            PopulateFieldsAndObjects(o);
        }
        using (VerticalScope()) {
            foreach (var field in _fieldsByObject[o]) {
                using (HorizontalScope()) {
                    Space(indent);
                    bool isEnum = typeof(Enum).IsAssignableFrom(field.Key.FieldType);
                    Label($"{field.Key.Name} ({(isEnum ? "Enum: " : "")}{field.Key.FieldType.Name})", Width(600));
                    FieldGUI(o, wouldBePatch, indent, field.Key.FieldType, field.Value, field.Key);
                }
            }
        }
    }
    public static void FieldGUI(object parent, PatchOperation wouldBePatch, int indent, Type type, object @object, FieldInfo info) {
        if (@object == null) {
            Label("null", Width(400));
            return;
        }
        if (typeof(Enum).IsAssignableFrom(type)) {
            if (!_toggleStates.TryGetValue((parent, info, @object), out var state)) {
                state = false;
            }
            DisclosureToggle(@object.ToString(), ref state, 400);
            _toggleStates[(parent, info, @object)] = state;
            if (state) {
                if (!_editStates.TryGetValue((parent, info), out var curValue)) {
                    curValue = 0;
                }
                var vals = Enum.GetValues(type).Cast<object>();
                var enumNames = vals.Select(val => val.ToString()).ToArray();
                var tmp = (int)curValue;
                var cellsPerRow = Math.Min(6, enumNames.Length);
                SelectionGrid(ref tmp, enumNames, cellsPerRow, Width(200 * cellsPerRow));
                _editStates[(parent, info)] = tmp;
                Space(20);
                ActionButton("Change", () => {
                    PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyPrimitive, info.Name, type, Enum.Parse(type, enumNames[tmp]), parent.GetType());
                    PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                    CurrentState.AddOp(op);
                    CurrentState.CreatePatchFromState().RegisterPatch();
                });
            }
        } else if (typeof(UnityEngine.Object).IsAssignableFrom(type)) {
            Label(@object.ToString(), Width(400));
            Label("Unity Object");
        } else if (typeof(IReferenceBase).IsAssignableFrom(type)) {
            Label(@object.ToString(), Width(400));
            Label("Reference");
        } else if (_primitiveTypes.Contains(type)) {
            Label(@object.ToString(), Width(400));
            if (!_editStates.TryGetValue((parent, info), out var curValue)) {
                curValue = "";
            }
            string tmp = (string)curValue;
            TextField(ref tmp, null, Width(300));
            _editStates[(parent, info)] = tmp;
            Space(20);
            ActionButton("Change", () => {
                object result = null;
                if (type == typeof(string)) {
                    result = tmp;
                } else {
                    var method = AccessTools.Method(type, "TryParse", [typeof(string), type.MakeByRefType()]);
                    object[] parameters = [tmp, Activator.CreateInstance(type)];
                    bool success = (bool)(method?.Invoke(null, parameters) ?? false);
                    if (success) {
                        result = parameters[1];
                    } else {
                        Space(20);
                        Label($"Failed to parse value {tmp} to type {type.Name}".Orange());
                    }
                }
                if (result != null) {
                    PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyPrimitive, info.Name, type, result, parent.GetType());
                    PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                    CurrentState.AddOp(op);
                    CurrentState.CreatePatchFromState().RegisterPatch();
                }
            });
        } else if (PatchToolUtils.IsListOrArray(type)) {
            int elementCount = 0;
            if (type.IsArray) {
                Array array = @object as Array;
                elementCount = array.Length;
            } else {
                IList list = @object as IList;
                elementCount = list.Count;
            }
            Label($"{elementCount} elements", Width(400));
            if (!_toggleStates.TryGetValue((parent, info, @object), out var state)) {
                state = false;
            }
            DisclosureToggle("Show elements", ref state, 200);
            _toggleStates[(parent, info, @object)] = state;
            if (state) {
                int index = 0;
                Space(-750);
                using (VerticalScope()) {
                    Label("");
                    foreach (var elem in @object as IEnumerable) {
                        ListItemGUI(wouldBePatch, parent, info, elem, index, indent);
                        index += 1;
                    }
                }
            }
        } else {
            Label(@object.ToString(), Width(400));
            if (!_toggleStates.TryGetValue((parent, info, @object), out var state)) {
                state = false;
            }
            DisclosureToggle("Show fields", ref state, 200);
            _toggleStates[(parent, info, @object)] = state;
            if (state) {
                PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyComplex, info.Name, null, null, parent.GetType());
                PatchOperation op = wouldBePatch.AddOperation(tmpOp);
                Space(-700);
                using (VerticalScope()) {
                    Label("");
                    NestedGUI(@object, op, indent + 50);
                }
            }
        }
    }
    public static void ListItemGUI(PatchOperation wouldBePatch, object parent, FieldInfo info, object elem, int index, int indent) {
        using (HorizontalScope()) {
            PatchOperation tmpOp = new(PatchOperation.PatchOperationType.ModifyCollection, info.Name, null, null, parent.GetType(), PatchOperation.CollectionPatchOperationType.ModifyAtIndex, index);
            PatchOperation op = wouldBePatch.AddOperation(tmpOp);
            using (VerticalScope()) {
                Label("");
                using (HorizontalScope()) {
                    FieldGUI(parent, op, indent + 50, elem.GetType(), elem, info);
                }
            }
        }
    }

    public static void PopulateFieldsAndObjects(object o) {
        Dictionary<FieldInfo, object> result = new();
        foreach (var field in PatchToolUtils.GetFields(o.GetType())) {
            result[field] = field.GetValue(o);
        }
        _fieldsByObject[o] = result;
    }
}
