using System;
using System.Collections.Generic;
using System.Xml;
using Verse;

namespace CombatExtended
{
	public class PatchOperationWrap : PatchOperation
    {
        private const string formatNodeName = "fi_";
        protected string xpath;
        protected List<string> nodes;
        protected XmlContainer with;
        
        private List<XmlNode> tempNodes = new List<XmlNode>();

        public override bool ApplyWorker(XmlDocument xml)
        {
            bool patched = false;
            foreach (XmlNode node in xml.SelectNodes(xpath))
            {
                patched = true;
                Patch(node, xml);                
            }
            tempNodes.Clear();
            return patched;
        }

        private void Patch(XmlNode root, XmlDocument document)
        {
            tempNodes.Clear();            
            XmlNode wrapper = document.ImportNode(with.node.ChildNodes[0], true);
            bool wrapperInserted = false;
            foreach (string path in nodes)
            {
                XmlNodeList subNodes = root.SelectNodes(path);
                if (subNodes.Count == 0)
                {
                    Log.Error($"CE: PatchOperationWrap didn't find all nodes {path} to be wrapped in {with.node.ToString()}");
                    return;
                }
                XmlNode child = subNodes[0];
                tempNodes.Add(child);                
                if (!wrapperInserted)
                {
                    root.ReplaceChild(wrapper, child);
                    wrapperInserted = true;
                }
                else
                {
                    root.RemoveChild(child);
                }
            }            
            for (int i = 0; i < tempNodes.Count; i++)
            {
                XmlNodeList formatingNodes = wrapper.SelectNodes("//" + formatNodeName + $"{i + 1}");
                if (formatingNodes.Count == 0)
                {
                    Log.Error($"CE: PatchOperationWrap didn't find any format string for index {i} for {xpath} to be wrapped in {with.node.ToString()}");
                    return;
                }
                foreach (XmlNode node in formatingNodes)
                {
                    node.ParentNode.ReplaceChild(document.ImportNode(tempNodes[i].Clone(), true), node);
                }
            }
        }
    }
}

