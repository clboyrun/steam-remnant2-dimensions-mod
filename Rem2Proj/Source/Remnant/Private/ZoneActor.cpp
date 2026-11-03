#include "ZoneActor.h"
#include "AIDirector.h"
#include "AmbientSpawnManager.h"
#include "EncounterManager.h"
#include "MapGenComponent.h"
#include "ResourceSpawnManager.h"
#include "PersistenceComponent.h"
#include "Net/UnrealNetwork.h"

AZoneActor::AZoneActor(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer) {
    this->bAlwaysRelevant = true;
    this->bReplicates = true;
    const FProperty* p_RemoteRole = GetClass()->FindPropertyByName("RemoteRole");
    (*p_RemoteRole->ContainerPtrToValuePtr<TEnumAsByte<ENetRole>>(this)) = ROLE_SimulatedProxy;
    this->NetDormancy = DORM_DormantAll;
    this->MapGen = CreateDefaultSubobject<UMapGenComponent>(TEXT("MapGen"));
    this->AIDirector = CreateDefaultSubobject<UAIDirector>(TEXT("AIDirector"));
    this->EncounterManager = CreateDefaultSubobject<UEncounterManager>(TEXT("EncounterManager"));
    this->AmbientSpawnManager = CreateDefaultSubobject<UAmbientSpawnManager>(TEXT("AmbientSpawnManager"));
    this->ResourceSpawnManager = CreateDefaultSubobject<UResourceSpawnManager>(TEXT("ResourceSpawnManager"));
    this->PersistenceComponent = CreateDefaultSubobject<UPersistenceComponent>(TEXT("Persistence"));
    this->ID = -1;
    this->Level = 1;
    this->LevelMin = 0;
    this->LevelMax = 100;
    this->ZoneLevel = 1;
    this->ItemLevel = 1;
    this->ParentZoneID = -1;
    this->QuestID = -1;
    this->Seed = -1;
    this->TileSet = NULL;
    this->DayLength = 1.00f;
    this->DayOffset = 0.00f;
    this->Static = false;
    this->ZoneLevelInitialized = false;
}

void AZoneActor::UpdateLevelScaling(bool Force) {
}

void AZoneActor::UnloadZone() {
}

void AZoneActor::SetZoneLabel(FText NewLabel) {
}

void AZoneActor::OnResourcesLoad() {
}

void AZoneActor::OnRep_ServerState() {
}

void AZoneActor::OnRep_Level() {
}

void AZoneActor::OnMapGenInitialized() {
}

void AZoneActor::OnMapGenFullyLoaded() {
}

void AZoneActor::OnMapGenFailed() {
}

void AZoneActor::OnFinalizeLoad() {
}

void AZoneActor::LoadZone(FName Link) {
}

bool AZoneActor::IsLoaded() {
    return false;
}

bool AZoneActor::IsFullyUnloaded() const {
    return false;
}

bool AZoneActor::IsChildZone() const {
    return false;
}

bool AZoneActor::HasFailedLoad() const {
    return false;
}

EZoneStatus AZoneActor::GetZoneStatus() const {
    return EZoneStatus::Unloaded;
}

int32 AZoneActor::GetZoneLevel() {
    return 0;
}

FString AZoneActor::GetUniqueName() const {
    return TEXT("");
}

float AZoneActor::GetTOD() {
    return 0.0f;
}

ARemnantQuest* AZoneActor::GetQuest() {
    return NULL;
}

AZoneActor* AZoneActor::GetParentZone() {
    return NULL;
}

int32 AZoneActor::GetItemLevel() {
    return 0;
}

bool AZoneActor::ContainsActor(AActor* Actor) {
    return false;
}

void AZoneActor::GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const {
    Super::GetLifetimeReplicatedProps(OutLifetimeProps);
    
    DOREPLIFETIME(AZoneActor, ID);
    DOREPLIFETIME(AZoneActor, Level);
    DOREPLIFETIME(AZoneActor, QuestID);
    DOREPLIFETIME(AZoneActor, NameID);
    DOREPLIFETIME(AZoneActor, Label);
    DOREPLIFETIME(AZoneActor, Seed);
    DOREPLIFETIME(AZoneActor, Static);
    DOREPLIFETIME(AZoneActor, ZoneLinks);
    DOREPLIFETIME(AZoneActor, DynamicResources);
    DOREPLIFETIME(AZoneActor, ServerState);
}


