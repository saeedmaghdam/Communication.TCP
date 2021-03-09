Steps to create a server:
1. Inject ITcpServerBuilder
2. Create a TcpServer using builder:
	var tcpServer = tcpServerBuilder.IPAddress("127.0.0.1").Port(11000).Build();
3. Start server:
	await tcpServer.StartAsync(cancellationToken);


Steps to create a client:
1. Inject ITcpClientBuilder
2. Create a Tcp Client using builder:
	tcpClient = tcpClientBuilder.Create().IPAddress("127.0.0.1").Port(11000).Build();
3. Send a packet to server:
	await tcpClient.SendCommandAsync(0x00, BitConverter.GetBytes("This is a test message"), cancellationToken);