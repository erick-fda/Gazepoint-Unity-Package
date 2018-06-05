/*========================================================================================
    GazepointClient                                                                  *//**
	
    A client for interfacing with an active Gazepoint 2.0 server instance running on the 
    same machine. 
	
    Copyright 2018 Erick Fernandez de Arteaga. All rights reserved.
        https://www.linkedin.com/in/erick-fda
        https://github.com/erick-fda
        https://bitbucket.org/erick-fda
	
*//*====================================================================================*/

using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;

namespace GazepointUnityPackage
{
public class GazepointClient : MonoBehaviour
{
	/*----------------------------------------------------------------------------------------
		Instance Fields
	----------------------------------------------------------------------------------------*/
	/* Client data */
    [SerializeField] private int _serverPort = 4242;
    [SerializeField] private string _serverAddress = "127.0.0.1"; 
    private TcpClient _client;
    private NetworkStream _dataStream;
    private StreamWriter _dataWriter;
    private string _dataIn;
    
    /* Data record types */
    public GazepointRecordType RECORD_CURSOR = new GazepointRecordType("CURSOR");
    public GazepointRecordType RECORD_DATA = new GazepointRecordType("DATA");
    public GazepointRecordType RECORD_POG_FIX = new GazepointRecordType("POG_FIX");
    public GazepointRecordType RECORD_TIME = new GazepointRecordType("TIME");
    private List<GazepointRecordType> _recordTypes;

    /* Data reading options */
    [SerializeField] private bool _enableReadCursor = false;
    [SerializeField] private bool _enableReadData = false;
    [SerializeField] private bool _enableReadPogFix = false;
    [SerializeField] private bool _enableReadTime = false;

    /* String constants */
    private const string RECORD_START_FLAG = "<REC";
    private const string RECORD_END_FLAG = "\r\n";
    private const string ENABLE_SEND = "<SET ID=\"ENABLE_SEND_{0}\" STATE=\"{1}\" />\r\n";

	/*----------------------------------------------------------------------------------------
		Instance Properties
	----------------------------------------------------------------------------------------*/
	
    
	/*----------------------------------------------------------------------------------------
		MonoBehaviour Methods
	----------------------------------------------------------------------------------------*/
    /**
        Initialize client connection and record reading.
    */
    private void Start()
    {
        /* Try creating client and report error if any. */
        try
        {
            _client = new TcpClient(_serverAddress, _serverPort);
        }
        catch (Exception e)
        {
            Debug.LogError("GazepointClient.Start(): Failed to create client with error: " + e);
        }

        /* Set up writer. */
        _dataStream = _client.GetStream();
        _dataWriter = new StreamWriter(_dataStream);

        InitRecordReading();

        _dataWriter.Flush();
    }

    /**
        Read enabled records.
    */
    private void Update()
    {
        //Debug.Log("loop");
        int ch = _dataStream.ReadByte();
        if (ch != -1)
        {
            _dataIn += (char)ch;
            
            /* If a full record has been read... */
            if (_dataIn.IndexOf(RECORD_END_FLAG) != -1)					
            {
                /* If the record is a data record... */
                if (_dataIn.IndexOf(RECORD_START_FLAG) != -1)
                {
                    Debug.Log("data here");
                }

                _dataIn = "";
            }
        }
    }

    /**
        Close client connection.
    */
    private void OnDestroy()
    {
        _dataWriter.Close();
        _dataStream.Close();
        _client.Close();
    }
    
	/*----------------------------------------------------------------------------------------
		Instance Methods
	----------------------------------------------------------------------------------------*/
    /**
        Initialize the reading of enabled records.
    */
    private void InitRecordReading()
    {
        SetRead(RECORD_CURSOR, _enableReadCursor);
        SetRead(RECORD_DATA, _enableReadData);
        SetRead(RECORD_POG_FIX, _enableReadPogFix);
        SetRead(RECORD_TIME, _enableReadTime);
    }

    /**
        Populate the record types list with all record types.
    */
    private void InitRecordTypesList()
    {
        _recordTypes.Add(RECORD_CURSOR);
        _recordTypes.Add(RECORD_DATA);
        _recordTypes.Add(RECORD_POG_FIX);
        _recordTypes.Add(RECORD_TIME);
    }

    /**
        Set whether to read records of the given type.
    */
	public void SetRead(GazepointRecordType record, bool read)
    {
        _dataWriter.Write(string.Format(ENABLE_SEND, record.Name, read ? 1 : 0));
    }

    /**
        Set whether to read all records.

        Reading records affects performance. Do not enable reading all records unless you 
        are actually using the data. This function is primarily provided for the purpose 
        of allowing users to easily DISABLE reading all records.
    */
    public void SetReadAll(bool read)
    {
        foreach (GazepointRecordType eachRecord in _recordTypes)
        {
            _dataWriter.Write(string.Format(ENABLE_SEND, eachRecord.Name, read ? 1 : 0));
        }
    }

    /*----------------------------------------------------------------------------------------
		Private Classes
	----------------------------------------------------------------------------------------*/
	/**
        Defines a data record type.
    */
    public class GazepointRecordType
    {
        public readonly string Name;
        public GazepointRecordType (string name) { Name = name; }
    }
}}
