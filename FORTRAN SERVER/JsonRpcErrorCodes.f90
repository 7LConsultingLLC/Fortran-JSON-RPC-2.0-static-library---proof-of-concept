!****************************************************************************
!
!  MODULE: JsonRpcErrorCodes
!
!  PURPOSE:  JSON-RPC 2.0 error code constants and arithmetic op-code
!            constants shared across all handler submodules.
!
!  REFERENCE: JSON-RPC 2.0 specification defines:
!               -32700         Parse error
!               -32600         Invalid request
!               -32601         Method not found
!               -32602         Invalid params
!               -32603         Internal error
!               -32000 to -32099  Reserved for implementation-defined errors
!
!****************************************************************************
module JsonRpcErrorCodes
    implicit none

    ! Standard JSON-RPC 2.0 error codes (-32700 to -32600)
    integer, parameter :: JSONRPC_PARSE_ERROR      = -32700
    integer, parameter :: JSONRPC_INVALID_REQUEST  = -32600
    integer, parameter :: JSONRPC_METHOD_NOT_FOUND = -32601
    integer, parameter :: JSONRPC_INVALID_PARAMS   = -32602
    integer, parameter :: JSONRPC_INTERNAL_ERROR   = -32603

    ! Server-defined error codes (-32000 to -32099, reserved range)
    integer, parameter :: JSONRPC_SERVER_ERROR     = -32000
    integer, parameter :: JSONRPC_MATRIX_DIMENSION_MISMATCH = -32001
    integer, parameter :: JSONRPC_INVALID_DATA_TYPE         = -32002
    integer, parameter :: JSONRPC_OVERFLOW_ERROR   = -32098
    integer, parameter :: JSONRPC_DIVISION_BY_ZERO = -32099

    ! Operation codes used by PerformArithmeticOp, PerformRealArithmeticOp,
    ! and PerformComplexArithmeticOp to select the arithmetic operation.
    integer, parameter :: OP_ADD      = 1
    integer, parameter :: OP_SUBTRACT = 2
    integer, parameter :: OP_MULTIPLY = 3

    ! 32-bit signed integer limits, used for overflow detection in PerformArithmeticOp
    integer, parameter :: INT_MAX = 2147483647
    integer, parameter :: INT_MIN = -2147483648

end module JsonRpcErrorCodes