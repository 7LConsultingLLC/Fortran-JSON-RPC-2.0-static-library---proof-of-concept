' =========================================================================
' ClientInfo.vb
' Data class returned by RpcTargetHandler.GetClientInfo() when the server
' requests information about the connected client.  StreamJsonRpc serialises
' this object to JSON automatically when it is returned from a target method.
' =========================================================================
Public Class ClientInfo
    Public Property Name As String
    Public Property Version As String
    Public Property Platform As String
    Public Property Runtime As String
End Class
