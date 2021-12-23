using System;
using Verse;
using Verse.AI;

namespace CombatExtended.AI
{
	public class ThinkNode_SequenceStack : ThinkNode_Priority
	{
		/*
		 *  How ThinkNode_Prepend works:
		 * 
		 * TODO - add some text.
		 * 
		 */

		public override ThinkResult TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams)
		{
			JobQueue queue = pawn.jobs.jobQueue;
			ThinkResult lastResult = ThinkResult.NoJob;
			int count = subNodes.Count;
			for (int i = 0; i < count; i++)
			{
				ThinkResult result = ThinkResult.NoJob;
				try
				{
					result = subNodes[i].TryIssueJobPackage(pawn, jobParams);
				}
				catch (Exception ex)
				{
					Log.Error(string.Concat("Exception in ", GetType(), " TryIssueJobPackage: ", ex.ToString()));
				}
				if (!result.IsValid)
				{
					return lastResult;
				}
				if (lastResult.IsValid)
				{
					queue.EnqueueFirst(lastResult.Job, lastResult.Tag);
                }
				lastResult = result;
			}
			return lastResult;
		}
	}
}

