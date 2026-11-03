#include "CompanionSubsystem.h"

UCompanionSubsystem::UCompanionSubsystem() {
}

void UCompanionSubsystem::RemoveCompanionListener(const UObject* WorldContextObject, const FCompanionDelegate& Event) {
}

TArray<ACharacterGunfire*> UCompanionSubsystem::GetCompanions(const UObject* WorldContextObject) {
    return TArray<ACharacterGunfire*>();
}

void UCompanionSubsystem::AddCompanionListener(const UObject* WorldContextObject, const FCompanionDelegate& Event) {
}


