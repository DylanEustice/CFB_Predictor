% Read in network
clear;clc;
path = '../Networks/';
file = 'Network_2.csv';
data = xlsread([path  file]);

% Get layer sizes from top line
ann.LayerSizes = data(1,:);
ann.LayerSizes = ann.LayerSizes(~isnan(ann.LayerSizes));
ann.LayerSizes(ann.LayerSizes > 1) = ann.LayerSizes(ann.LayerSizes > 1) + 1;
ann.Header = data(4,:);
ann.Header = ann.Header(~isnan(ann.Header));

% Create layers
line = 5;
ann.Layers = cell(length(ann.LayerSizes)-1, 1);
for i = 1:length(ann.LayerSizes)-1
    ann.Layers{i}.Nodes = cell(ann.LayerSizes(i), 1);
    for j = 1:ann.LayerSizes(i)
        line = line + 1;
        weights = data(line,:); weights = weights(~isnan(weights));
        ann.Layers{i}.Nodes{j}.Weights = weights;
        ann.Layers{i}.Nodes{j}.Value = 0;
    end
end

step = 0.01;
outputs = zeros(100, ann.LayerSizes(1)-1); % minus 1 for bias node
for i = 1:ann.LayerSizes(1)-1
    index = 0;
    for j = step:step:1
        index = index + 1;
        input = zeros(ann.LayerSizes(1)-1, 1);
        input(i) = j;
        outputs(index,i) = ANN_Think(input, ann);
    end
end
plot(outputs);
C = regexp(sprintf('%d#', ann.Header), '#', 'split');
C(end) = [];
legend(C);

% sum differences
difSum = sum(abs(outputs(end,:)));
for i = 1:ann.LayerSizes(1)-1
    fprintf('%s: %f\n', C{i}, 100*outputs(end,i)/difSum);
end
