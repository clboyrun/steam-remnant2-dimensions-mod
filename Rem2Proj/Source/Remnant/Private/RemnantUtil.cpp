#include "RemnantUtil.h"

URemnantUtil::URemnantUtil() {
}

bool URemnantUtil::IsZoneLinkValid(const FZoneLinkInfo& ZoneLink) {
    return false;
}

bool URemnantUtil::IsValueEncodedReleaseHours(int32 Value, int32 SigDigits) {
    return false;
}

bool URemnantUtil::IsGameSessionReady(const UObject* WorldContextObject) {
    return false;
}

bool URemnantUtil::IsGameSeedOverridden() {
    return false;
}

AActor* URemnantUtil::GetZoneLinkActor(const FZoneLinkInfo& ZoneLink) {
    return NULL;
}

FTimespan URemnantUtil::GetYearsSinceStudioOpen() {
    return FTimespan{};
}

FTimespan URemnantUtil::GetTimeSinceRelease(int32 AdditionalHourDelta) {
    return FTimespan{};
}

ULevel* URemnantUtil::GetRemnantMapGenLevelForActor(AActor* Actor) {
    return NULL;
}

UMapGenComponent* URemnantUtil::GetRemnantMapGenComponentForActor(AActor* Actor) {
    return NULL;
}

int32 URemnantUtil::GetGameSeed() {
    return 0;
}

int32 URemnantUtil::GetEncodingForTimespan(FTimespan Timespan, int32 SigDigits) {
    return 0;
}

bool URemnantUtil::GetBaseZoneLinkByLabel(const UObject* WorldContextObject, const FString& Label, FZoneLinkInfo& OutZoneLink) {
    return false;
}

bool URemnantUtil::GetBaseZoneLink(const UObject* WorldContextObject, FZoneLinkInfo& OutZoneLink) {
    return false;
}

int32 URemnantUtil::GetBaseZoneID() {
    return 0;
}

AZoneActor* URemnantUtil::GetBaseZoneActor(const UObject* WorldContextObject) {
    return NULL;
}

int32 URemnantUtil::EncodeHoursSinceRelease(int32 SigDigits) {
    return 0;
}

bool URemnantUtil::CanPlayerExitGame(const UObject* WorldContextObject) {
    return false;
}


