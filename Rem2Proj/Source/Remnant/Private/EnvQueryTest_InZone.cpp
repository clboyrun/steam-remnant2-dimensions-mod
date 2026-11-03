#include "EnvQueryTest_InZone.h"

UEnvQueryTest_InZone::UEnvQueryTest_InZone() {
    this->TestPurpose = EEnvTestPurpose::Filter;
    this->FilterType = EEnvTestFilterType::Match;
    this->ScoringEquation = EEnvTestScoreEquation::Constant;
    this->ZonePadding = 200.00f;
}


