/*
* Copyright (c) CookApps.
*/

using System;
using Google.Protobuf;
using Tech.Hive.V1;

namespace CookApps.NetLite
{
    /// <summary>
    /// 모든 gRPC 메시지의 베이스 인터페이스입니다.
    /// Google Protobuf의 <see cref="IMessage"/>를 상속하여 gRPC 통신에 사용되는 메시지를 정의합니다.
    /// </summary>
    public interface IGrpcMessage : IMessage
    {
    }

    /// <summary>
    /// 모든 gRPC 응답 메시지의 베이스 인터페이스입니다.
    /// 응답 상태, 성공 여부, 예외 정보 등을 포함합니다.
    /// </summary>
    public interface IGrpcMessageResponse : IGrpcMessage
    {
        /// <summary>
        /// 서버로부터 받은 응답 상태 코드입니다.
        /// </summary>
        ResponseStatus Status { get; }

        /// <summary>
        /// 요청의 성공 여부를 나타냅니다.
        /// <c>false</c>인 경우, <see cref="Exception"/>을 통해 예외를 확인하거나
        /// <see cref="Status"/>를 통해 서버 응답 상태를 확인할 수 있습니다.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// 요청 처리 중 발생한 예외 정보입니다.
        /// 성공한 경우 <c>null</c>입니다.
        /// </summary>
        Exception Exception { get; init; }

        /// <summary>
        /// <see cref="IsSuccess"/>가 <c>false</c>인 경우 <see cref="Exception"/>을 발생시킵니다.
        /// 오류 처리를 간편하게 하기 위한 헬퍼 메서드입니다.
        /// </summary>
        void ThrowIfError();
    }

    /// <summary>
    /// 모든 gRPC 요청 메시지의 베이스 인터페이스입니다.
    /// </summary>
    public interface IGrpcMessageRequest : IGrpcMessage
    {

    }

    /// <summary>
    /// 제네릭 타입을 지원하는 gRPC 요청 메시지 인터페이스입니다.
    /// </summary>
    /// <typeparam name="T">요청 메시지의 구체적인 타입</typeparam>
    public interface IGrpcMessageRequest<T> : IGrpcMessageRequest where T : IGrpcMessageRequest
    {
    }

    /// <summary>
    /// 제네릭 타입을 지원하는 gRPC 응답 메시지 인터페이스입니다.
    /// </summary>
    /// <typeparam name="T">응답 메시지의 구체적인 타입</typeparam>
    public interface IGrpcMessageResponse<T> : IGrpcMessageResponse where T : IGrpcMessageResponse
    {
    }

    /// <summary>
    /// 요청과 응답 타입을 모두 지정하는 gRPC 요청 메시지 인터페이스입니다.
    /// </summary>
    /// <typeparam name="TRequest">요청 메시지의 구체적인 타입</typeparam>
    /// <typeparam name="TResponse">응답 메시지의 구체적인 타입</typeparam>
    public interface IGrpcMessageRequest<TRequest, TResponse> : IGrpcMessageRequest<TRequest>
        where TRequest : IGrpcMessageRequest
        where TResponse : IGrpcMessageResponse<TResponse>
    {
    }
}
