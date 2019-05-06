RegisterNetEvent('ss:registerPeerEvent')

AddEventHandler('ss:registerPeerEvent', function(name)
	RegisterNetEvent(name)

	AddEventHandler(name, function(...)
		TriggerClientEvent(name, -1, ...)
	end)
end)

-- variable spaces
varSpaces = {}

RegisterServerEvent('ocw:varSpace:reqSet')

AddEventHandler('ocw:varSpace:reqSet', function(spaceId, key, value)
	local space = varSpaces[spaceId]

	if space then
		space[key] = value
	end
end)

RegisterServerEvent('ocw:varSpace:resync')

AddEventHandler('ocw:varSpace:resync', function()
	local source = source

	for k, v in pairs(varSpaces) do
		TriggerClientEvent('ocw:varSpace:create', source, k, v.backing)
	end

	SetTimeout(5000, function()
		--TriggerClientEvent("ss:startAmbientEvent", source, 'wow')
	end)
end)

function CreateVariableSpace(idx)
	if varSpaces[idx] then
		return
	end

	varSpaces[idx] = {}

	TriggerClientEvent('ocw:varSpace:create', -1, idx, {})

	local backing = {}
	varSpaces[idx].backing = backing 

	setmetatable(varSpaces[idx], {
		__index = function(t, key)
			return backing[key]
		end,

		__newindex = function(t, key, value)
			print('sync-setting varspace', idx, key, value)

			TriggerClientEvent('ocw:varSpace:set', -1, idx, key, value)

			backing[key] = value
		end
	})

	return idx
end

RegisterServerEvent('ss:makeVarSpace')

AddEventHandler('ss:makeVarSpace', function(id)
	CreateVariableSpace(id)
end)