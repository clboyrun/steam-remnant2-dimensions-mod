#include "RemnantGameMode.h"
#include "Templates/SubclassOf.h"

ARemnantGameMode::ARemnantGameMode(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->BaseQuest = NULL;
    this->AutoLoadQuest = NULL;
    this->DebugQuest = NULL;
    this->DebugWaypointBP = NULL;
    this->DebugWithoutZone = false;
    this->DebugQuestStartZone = NULL;
    this->DebugQuestStartWaypoint = NULL;
    this->PendingDebugQuestInstance = NULL;
    this->DebugQuestInstance = NULL;
    this->FailResetDelay = 5.00f;
    this->MaxFailResetDelay = 20.00f;
    this->ZoneTravelDelay = 5.00f;
}

void ARemnantGameMode::TryRespawnPlayersPostDeath() {
}

void ARemnantGameMode::TravelToZone(FName Context, int32 DestZoneID, const FName& DestLink, AActor* Source, bool bForce, bool bInstant) {
}

void ARemnantGameMode::TravelToLastCheckpoint(FName Context, AActor* Source) {
}

void ARemnantGameMode::SetPostDeathScreenTime(float Time) {
}

void ARemnantGameMode::SetChunkDownloaded(const FString& PakPath) {
}

bool ARemnantGameMode::SetAsRootCheckpoint(const FZoneLinkInfo& NewZoneLink, bool IgnoreActive) {
    return false;
}

bool ARemnantGameMode::SetAsRespawnZoneLink(const FZoneLinkInfo& NewZoneLink, bool bIgnoreWorldState) {
    return false;
}

void ARemnantGameMode::RespawnDeadPlayersNearLivingPlayers(float RetryDelay) {
}

void ARemnantGameMode::OnStateDebugQuestLoaded(AQuest* Quest) {
}

void ARemnantGameMode::OnSpawnDebugQuestLoaded(AQuest* Quest) {
}

void ARemnantGameMode::OnFullGameAvailable() {
}

bool ARemnantGameMode::IsIntroQuestDisabled(const UObject* WorldContextObject) {
    return false;
}

bool ARemnantGameMode::IsIntroQuestComplete(const UObject* WorldContextObject) {
    return false;
}

bool ARemnantGameMode::IsEveryoneEntitled(TSubclassOf<UEntitlementType> RequiredEntitlement, bool ShowFailDialog, bool KickUnentitled) {
    return false;
}

void ARemnantGameMode::IncrementAllPlayersBossCounters() {
}

FZoneLinkInfo ARemnantGameMode::GetRespawnZoneLink() const {
    return FZoneLinkInfo{};
}

void ARemnantGameMode::ActivateCheckpoint(ACheckpoint* Checkpoint) {
}


