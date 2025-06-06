﻿using Harmony;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace Klyte.Commons.Utils
{
    public class LogUtils
    {
        #region Log Utils

        public static void DoLog(string format, params object[] args)
        {
            try
            {
                if (CommonProperties.DebugMode)
                {
                    Debug.LogFormat($"{CommonProperties.Acronym}v" + CommonProperties.Version + " " + format, args);
                }

            }
            catch
            {
                Debug.LogErrorFormat($"{CommonProperties.Acronym}: Erro ao fazer log: {{0}} (args = {{1}})", format, args == null ? "[]" : string.Join(",", args.Select(x => x != null ? x.ToString() : "--NULL--").ToArray()));
            }
        }
        public static void DoWarnLog(string format, params object[] args)
        {
            try
            {
                Debug.LogWarningFormat($"{CommonProperties.Acronym}v" + CommonProperties.Version + " " + format, args);
            }
            catch
            {
                Debug.LogErrorFormat($"{CommonProperties.Acronym}: Erro ao fazer warn log: {{0}} (args = {{1}})", format, args == null ? "[]" : string.Join(",", args.Select(x => x != null ? x.ToString() : "--NULL--").ToArray()));
            }
        }
        public static void DoErrorLog(string format, params object[] args)
        {
            try
            {
                Debug.LogErrorFormat($"{CommonProperties.Acronym}v" + CommonProperties.Version + " " + format, args);
            }
            catch
            {
                Debug.LogErrorFormat($"{CommonProperties.Acronym}: Erro ao fazer err log: {{0}} (args = {{1}})", format, args == null ? "[]" : string.Join(",", args.Select(x => x != null ? x.ToString() : "--NULL--").ToArray()));
            }
        }

        public static void PrintMethodIL(IEnumerable<CodeInstruction> inst, bool force = false)
        {
            if (force || CommonProperties.DebugMode)
            {
                int j = 0;
                Debug.Log($"TRANSPILLED:\n\t{string.Join("\n\t", inst.Select(x => $"{(j++).ToString("D8")} {x.opcode.ToString().PadRight(10)} {ParseOperand(inst, x.operand)}").ToArray())}");
            }
        }

        public static string GetLinesPointingToLabel(IEnumerable<CodeInstruction> inst, Label lbl)
        {
            int j = 0;
            return "\t" + string.Join("\n\t", inst.Select(x => Tuple.New(x, $"{(j++).ToString("D8")} {x.opcode.ToString().PadRight(10)} {ParseOperand(inst, x.operand)}")).Where(x => x.First.operand is Label label && label == lbl).Select(x => x.Second).ToArray());
        }


        internal static string ParseOperand(IEnumerable<CodeInstruction> instr, object operand)
        {
            if (operand is null)
            {
                return null;
            }

            if (operand is Label lbl)
            {
                return "LBL: " + instr.Select((x, y) => Tuple.New(x, y)).Where(x => x.First.labels.Contains(lbl)).Select(x => $"{x.Second.ToString("D8")} {x.First.opcode.ToString().PadRight(10)} {ParseOperand(instr, x.First.operand)}").FirstOrDefault() ?? "<UNKNOWN!!!>";
            }
            else
            {
                return operand.ToString() + $" (Type={operand.GetType()})";
            }
        }
        #endregion
    }
}
