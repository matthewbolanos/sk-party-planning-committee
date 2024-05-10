// Generate shorter custom IDs using UUIDv4
function generateCustomId() {
    return Math.random().toString(36).substring(4, 10); // Generates a 6-character random string
}

// Get database reference
const db = db.getSiblingDB("HomeAutomation");

// Generate custom IDs for smart devices
const lampId = generateCustomId();
const stageLeftId = generateCustomId();
const stageRightId = generateCustomId();
const speakerId = generateCustomId();

// Generate shorter custom ID for the room
const stageRoomId = generateCustomId();

// Insert base data into "SmartDevices" collection
db.SmartDevices.insertMany([
    { _id: lampId, Name: "Lamp", Type: "Light" },
    { _id: stageLeftId, Name: "Stage left", Type: "Light" },
    { _id: stageRightId, Name: "Stage right", Type: "Light" },
    { _id: speakerId, Name: "Beosound 2", Type: "Speaker" }
]);

// Insert light-specific data into "Lights" collection
db.Lights.insertMany([
    { _id: lampId, IsOn: false, HexColor: "#FFFFFF", Brightness: "Medium" },
    { _id: stageLeftId, IsOn: false, HexColor: "#FFFFFF", Brightness: "Medium" },
    { _id: stageRightId, IsOn: false, HexColor: "#FFFFFF", Brightness: "Medium" }
]);

// Insert speaker-specific data into "Speakers" collection
db.Speakers.insertOne({
    _id: speakerId,
    IsPlaying: false,
    CurrentSong: null,
    Volume: 100
});

// Insert data into "Homes" collection
db.Homes.insertOne({
    Name: "Smart Home",
    Rooms: [
        {
            _id: stageRoomId,
            Name: "Stage",
            DeviceIds: [
                lampId,
                stageLeftId,
                stageRightId,
                speakerId
            ]
        }
    ]
});
