﻿// Copyright (c) 2002-2017 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.IO;
using Neo4j.Driver.V1;
using Xunit;
using Xunit.Abstractions;
using static Neo4j.Driver.Tests.TcpSocketClientTestSetup;

namespace Neo4j.Driver.Tests.IO
{
    public class ChunkReaderTests
    {

        public class Constructor
        {

            [Fact]
            public void ShouldThrowArgumentNullOnNullStream()
            {
                var ex = Record.Exception(() => new ChunkReader(null));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldThrowArgumentNullOnNullStreamWithLogger()
            {
                var mockLogger = new Mock<ILogger>();

                var ex = Record.Exception(() => new ChunkReader(null, mockLogger.Object));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentNullException>();
            }

            [Fact]
            public void ShouldThrowArgumentOutOfRangeOnNotReadableStream()
            {
                var mockStream = new Mock<Stream>();
                mockStream.SetupGet(x => x.CanRead).Returns(false);

                var ex = Record.Exception(() => new ChunkReader(mockStream.Object));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentOutOfRangeException>();
            }

            [Fact]
            public void ShouldThrowArgumentOutOfRangeOnNotReadableStreamWithLogger()
            {
                var mockLogger = new Mock<ILogger>();
                var mockStream = new Mock<Stream>();
                mockStream.SetupGet(x => x.CanRead).Returns(false);

                var ex = Record.Exception(() => new ChunkReader(mockStream.Object, mockLogger.Object));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<ArgumentOutOfRangeException>();
            }

            [Fact]
            public void ShouldAcceptNullLogger()
            {
                var ex = Record.Exception(() => new ChunkReader(new MemoryStream(), null));

                ex.Should().BeNull();
            }

        }

        public class ReadNextMessagesMethod
        {

            [Theory]
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            public void ShouldReadMessageAcrossChunks(byte[] input, byte[] correctValue)
            {
                var reader = new ChunkReader(new MemoryStream(input));
                var targetStream = new MemoryStream();

                var count = reader.ReadNextMessages(targetStream);
                count.Should().Be(1);

                var real = targetStream.ToArray();

                real.Should().Equal(correctValue);
            }

        }

        public class ReadNextMessagesAsyncMethod
        {

            [Theory]
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x02, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            [InlineData(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x01, 0x01, 0x00, 0x01, 0x02, 0x00, 0x00 }, new byte[] { 0x00, 0x01, 0x02 })]
            public async void ShouldReadMessageAcrossChunks(byte[] input, byte[] correctValue)
            {
                var reader = new ChunkReader(new MemoryStream(input));
                var targetStream = new MemoryStream();

                var count = await reader.ReadNextMessagesAsync(targetStream);
                count.Should().Be(1);

                var real = targetStream.ToArray();

                real.Should().Equal(correctValue);
            }

            [Fact]
            public async void ShouldThrowWhenInnerTaskThrowsException()
            {
                var mockStream = new Mock<Stream>();
                mockStream.SetupGet(x => x.CanRead).Returns(true);
                mockStream.Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .Throws<InvalidOperationException>();
                var reader = new ChunkReader(mockStream.Object);

                var ex = await Record.ExceptionAsync(() => reader.ReadNextMessagesAsync(new MemoryStream()));

                ex.Should().NotBeNull();
                ex.Should().BeOfType<InvalidOperationException>();
            }

        }
        
    }
}
