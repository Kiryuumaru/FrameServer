#pragma once

template <typename TValue>
struct ArgsValue {
	bool matched;

	TValue value;

	void patch(ArgsValue argsValue) {
		matched = argsValue.matched;
		value = argsValue.value;
	}
};
