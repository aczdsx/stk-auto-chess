/*
* Copyright (c) CookApps.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace CookApps.AutoBattler.Editor
{
    /// <summary>
    /// Addressable 프로필을 관리하는 헬퍼 클래스입니다.
    /// </summary>
    public static class AddressableProfileHelper
    {
        private const string ProfileDev = "DEV";
        private const string ProfileProd = "PROD";
        private const string ProfileDefault = "Default";
        private const string DevDefineSymbol = "__DEV";
        private const string AmazonDefineSymbol = "UNITY_AMAZON";

        /// <summary>
        /// 빌드 타겟 (iOS, Android, Amazon....)
        /// </summary>
        public static string BuildTarget
        {
            get
            {
                BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
                if (activeBuildTarget != UnityEditor.BuildTarget.Android)
                {
                    return activeBuildTarget.ToString();
                }

                // Android 플랫폼인 경우, Amazon 스토어용인지 확인
                return HasScriptingDefineSymbol(AmazonDefineSymbol) ? "Amazon" : "Android";
            }
        }

        /// <summary>
        /// 타겟 프로필 (DEV 또는 PROD)
        /// </summary>
        public static string TargetProfile =>
            HasScriptingDefineSymbol(DevDefineSymbol) ? ProfileDev : ProfileProd;

        /// <summary>
        /// 주어진 define symbol이 현재 빌드 설정에 포함되는지 확인합니다.
        /// </summary>
        private static bool HasScriptingDefineSymbol(string symbol)
        {
            return GetScriptingDefineSymbols().Contains(symbol);
        }

        /// <summary>
        /// 현재 빌드 설정에서 정의된 스크립팅 define symbol들의 목록을 가져옵니다.
        /// </summary>
        /// <returns>스크립팅 define symbol들의 읽기 전용 컬렉션</returns>
        private static IReadOnlyCollection<string> GetScriptingDefineSymbols()
        {
            try
            {
                var namedTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                string symbols = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                return new HashSet<string>(
                    symbols.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(symbol => symbol.Trim()),
                    StringComparer.Ordinal);
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}