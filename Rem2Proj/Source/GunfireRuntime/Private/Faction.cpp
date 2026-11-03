#include "Faction.h"
#include "Templates/SubclassOf.h"

UFaction::UFaction() {
    this->InheritAffiliations = NULL;
    this->AffiliationTowardsSameFaction = EAffiliation::Friendly;
    this->ApplyFriendlyFireDamageScalar = true;
}

EAffiliation UFaction::GetAffiliationForActors(const AActor* Actor1, const AActor* Actor2) {
    return EAffiliation::SuperFriendly;
}

EAffiliation UFaction::GetAffiliation(TSubclassOf<UFaction> Faction1, TSubclassOf<UFaction> Faction2) {
    return EAffiliation::SuperFriendly;
}


