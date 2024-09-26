using System.Linq;
using Microsoft.CodeAnalysis;

namespace Generator.UserData
{

    internal static class DiagnosticError
    {
        /// partial 선언 누락 오류
        public static Diagnostic CreateAttributeNullError(ISymbol symbol)
        {
            var diagnostic = Diagnostic.Create(new DiagnosticDescriptor(
                    id: "COOKAPPS_GRPC_001",
                    title: "Class must be partial",
                    messageFormat: "Class '{0}'는 'partial' 선언 되어야 합니다.",
                    category: "CookAppsGrpcServiceGenerator",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                symbol.Locations.FirstOrDefault(),
                symbol.Name);
            return diagnostic;
        }

        /// 해당 속성의 서비스 타입이 ClientBase를 상속하지 않는 오류
        public static Diagnostic CreateClientBaseError(ISymbol symbol)
        {
            var diagnostic = Diagnostic.Create(new DiagnosticDescriptor(
                    id: "COOKAPPS_GRPC_002",
                    title: "ServiceType must inherit from Grpc.Core.ClientBase",
                    messageFormat: "Class '{0}'의 서비스 타입은 Grpc.Core.ClientBase를 상속받아야 합니다.",
                    category: "CookAppsGrpcServiceGenerator",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true),
                symbol.Locations.FirstOrDefault(),
                symbol.Name);
            return diagnostic;
        }

    }
}
