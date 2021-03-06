﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Base;
using Model;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyEditor
{
	public class BehaviourTreeNodeProxy<T> where T : Node
	{
		public T Node;
		public NodeProto Proto;

		public BehaviourTreeNodeProxy(NodeProto p)
		{
			Node = (T) Activator.CreateInstance(typeof (T), p);
			Proto = p;
			UpdateData();
		}

		public void SetDescribe(string desc)
		{
			Proto.describe = desc;
		}

		public void Apply()
		{
			FieldInfo[] mems = Node.GetType().GetFields();
			foreach (FieldInfo mem in mems)
			{
				Proto.args_dict.SetKeyValueComp(mem.FieldType, mem.Name, mem.GetValue(Node));
			}
		}

		public NodeProto GetNodeProtoCopy()
		{
			return BehaviorTreeOperateUtility.NodeProtoParamClone(Proto);
		}

		public void UpdateData()
		{
			foreach (var argsItem in Proto.args_dict)
			{
				FieldInfo fieldInfo = typeof (T).GetField(argsItem.Key, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				fieldInfo.SetValue(Node, argsItem.Value.GetValueByType(fieldInfo.FieldType));
			}
		}
	}

	public static class BehaviorTreeOperateUtility
	{
		/// <summary>
		/// 克隆Node Proto的参数队列
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public static NodeProto NodeProtoParamClone(NodeProto p)
		{
			NodeProto newP = new NodeProto();
			foreach (var dict in p.args_dict)
			{
				string key = dict.Key;
				ValueBase value = ValueBase.Clone(dict.Value);
				newP.args_dict.Add(key, value);
			}
			return newP;
		}

		public static GameObject CreateTreeWithType<T>(string path, string desc = "默认行为树")
		{
			desc = desc.Replace("BT_", "");
			return CreateTree(path, typeof (T).Name, desc);
		}

		public static GameObject CreateTree(string path, string rootNodeName, string desc = "默认行为树")
		{
			GameObject prefab = null;
			try
			{
				GameObject go = new GameObject();
				go.name = desc;
				prefab = PrefabUtility.CreatePrefab($"{path}/BT_{desc}.prefab", go, ReplacePrefabOptions.ReplaceNameBased);
				BehaviorTreeConfig config = prefab.AddComponent<BehaviorTreeConfig>();
				config.AddRootNode(rootNodeName);
				EditorUtility.SetDirty(config);
				EditorUtility.SetDirty(prefab);
				Object.DestroyImmediate(go);
			}
			catch (Exception ex)
			{
				Log.Error($"创建行为树失败|{ex}");
			}
			return prefab;
		}

		public static GameObject CreateNewTree(string path, string rootNodeName, string desc = "默认行为树")
		{
			GameObject prefab = null;
			try
			{
				GameObject go = new GameObject();
				BehaviorTreeConfig newConfig = go.AddComponent<BehaviorTreeConfig>();
				BehaviorNodeConfig root = newConfig.AddRootNode(rootNodeName);

				prefab = PrefabUtility.CreatePrefab($"{path}/BT_{desc}.prefab", go, ReplacePrefabOptions.ReplaceNameBased);
				EditorUtility.SetDirty(newConfig);
				EditorUtility.SetDirty(prefab);
				Object.DestroyImmediate(go);
			}
			catch (Exception ex)
			{
				Log.Error($"创建行为树失败|{ex}");
			}
			return prefab;
		}

		private static void SortBehaviorTree(BehaviorNodeConfig nodeConfig)
		{
		}

		public static BehaviourTreeNodeProxy<T> AddNodeToLast<T>(BehaviorTreeConfig tree) where T : Node
		{
			BehaviorNodeConfig parent = tree.RootNodeConfig;
			string name = typeof (T).Name;
			BehaviorNodeConfig p = tree.AddChild(parent, name);
			BehaviourTreeNodeProxy<T> proxy = new BehaviourTreeNodeProxy<T>(p.ToNodeProto());
			return proxy;
		}

		public static BehaviourTreeNodeProxy<T> CreateNode<T>() where T : Node
		{
			NodeProto p = new NodeProto();
			p.name = typeof (T).Name;
			BehaviourTreeNodeProxy<T> proxy = new BehaviourTreeNodeProxy<T>(p);
			return proxy;
		}

		/// <summary>
		/// 从父节点获取某个指定名字的子节点
		/// </summary>
		/// <param name="list"></param>
		/// <param name="name"></param>
		/// <param name="p"></param>
		/// <returns></returns>
		public static bool TryGetActionChild(List<NodeProto> list, string name, out NodeProto p)
		{
			foreach (NodeProto proto in list)
			{
				if (proto.describe == name)
				{
					p = proto;
					return true;
				}
			}
			p = null;
			return false;
		}

		public static BTEditorTree Open(GameObject go)
		{
			BehaviorTreeConfig config = go.GetComponent<BehaviorTreeConfig>();
			return new BTEditorTree(config);
		}
	}
}