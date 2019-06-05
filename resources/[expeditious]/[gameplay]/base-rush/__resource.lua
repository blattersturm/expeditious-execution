resource_manifest_version '44febabe-d386-4d18-afbe-5e627f4af937'

resource_type 'gametype' {
    name = 'Base Rush'
}

file 'client/bin/Release/**/publish/*.dll'

client_script 'client/bin/Release/**/publish/*.net.dll'
server_script 'server/bin/Release/**/publish/*.net.dll'

--shared_script 'varman.js'