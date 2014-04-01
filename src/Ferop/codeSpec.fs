﻿module internal Ferop.CodeSpec

open System

open Ferop.Code
open Ferop.Core
open Ferop.Helpers

type CodeSpec = {
    Includes: string
    FunctionName: string
    ReturnType: Type
    Parameters: Parameter list
    Body: string }

let makeCodeSpec includes = function
    | Inline { Name = name; ReturnType = returnType; Parameters = parameters; Code = code } ->
        {
        Includes = includes
        FunctionName = name
        ReturnType = returnType
        Parameters = parameters
        Body = code }
    | Extern { Name = name; ReturnType = returnType; Parameters = parameters } ->
        {
        Includes = includes
        FunctionName = name
        ReturnType = returnType
        Parameters = parameters
        Body = "" }

let definePInvokeOfCodeSpec tb dllName codeSpec =
    definePInvokeMethod tb dllName codeSpec.FunctionName codeSpec.FunctionName codeSpec.ReturnType codeSpec.Parameters

// ********************************************
//      C
// ********************************************

let stdTypes =
    [
    (typeof<byte>,      "uint8_t")
    (typeof<sbyte>,     "int8_t")
    (typeof<uint16>,    "uint16_t")
    (typeof<int16>,     "int16_t")
    (typeof<uint32>,    "uint32_t")
    (typeof<int>,       "int32_t")
    (typeof<uint64>,    "uint64_t")
    (typeof<int64>,     "int64_t")
    (typeof<single>,    "float")
    (typeof<double>,    "double")]

let returnTypes =
    stdTypes @
    [
    (typeof<Void>,      "void")]

let parameterTypes =
    stdTypes @
    [
    (typeof<nativeptr<byte>>,   "uint8_t *")
    (typeof<nativeptr<sbyte>>,  "int8_t *")
    (typeof<nativeptr<uint16>>, "uint16_t *")
    (typeof<nativeptr<int16>>,  "int16_t *")
    (typeof<nativeptr<uint32>>, "uint32_t *")
    (typeof<nativeptr<int>>,    "int32_t *")
    (typeof<nativeptr<uint64>>, "uint64_t *")
    (typeof<nativeptr<int64>>,  "int64_t *")
    (typeof<nativeptr<single>>, "float *")
    (typeof<nativeptr<double>>, "double *")
    (typeof<nativeint>,         "void *")]

let defaultCode =
    @"
#include <stdint.h>

#if defined(_WIN32)
#   define FEROP_IMPORT       __declspec(dllimport)
#   define FEROP_EXPORT       __declspec(dllexport)
#   define FEROP_DECL         __cdecl
#elif defined(__GNUC__)
#   define FEROP_EXPORT       __attribute__((visibility(""default"")))
#   define FEROP_IMPORT
#   define FEROP_DECL         __attribute__((cdecl))
#else
#   error Compiler not supported.
#endif
"

let codeSpecf =
    sprintf """
%s
%s

FEROP_EXPORT %s FEROP_DECL %s (%s)
{
    %s
}
"""
            defaultCode

let findReturnType typ =
    match
        returnTypes
        |> List.tryFind (fun (x, _) -> x = typ)
        with
    | None ->
        match Type.isUnmanaged typ with
        | true -> typ.Name
        | _ -> "Invalid return type."
    | Some (_, x) -> x

let findParameterType typ =
    match
        parameterTypes
        |> List.tryFind (fun (x, _) -> x = typ)
        with
    | None ->
        match Type.isUnmanaged typ with
        | true -> typ.Name
        | _ -> "Invalid parameter type."
    | Some (_, x) -> x

let generateParameters = function
    | [] -> ""
    | parameters ->

    parameters
    |> List.map (fun ({ Name = name; Type = typ }) ->
        let ctype = findParameterType typ
        sprintf "%s %s" ctype (name.Replace (" ", "_")))
    |> List.reduce (fun x y -> sprintf "%s, %s" x y)

let generateCode codeSpec =
    codeSpecf
            codeSpec.Includes
            (findReturnType codeSpec.ReturnType)
            codeSpec.FunctionName
            (generateParameters codeSpec.Parameters)
            codeSpec.Body
